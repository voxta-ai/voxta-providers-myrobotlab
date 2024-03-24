using System.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Voxta.Model.Shared;
using Voxta.Model.WebsocketMessages.ClientMessages;
using Voxta.Model.WebsocketMessages.ServerMessages;
using Voxta.Providers.Host;

namespace Voxta.Providers.MyRobotLab.Providers;

[UsedImplicitly]
public class MyRobotLabProvider(
    IRemoteChatSession session,
    IHttpClientFactory httpClientFactory,
    IVoxtaActionsYamlRepository repository,
    ITextToSpeechPreprocessor textToSpeechPreprocessor,
    IOptions<MyRobotLabOptions> options,
    ILogger<MyRobotLabProvider> logger
) : ProviderBase(session, logger)
{
    private const string ContextKey = "MyRobotLab";
    
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();
    
    protected override async Task OnStartAsync()
    {
        await base.OnStartAsync();

        var actions = await repository.DeserializeAsync();
        
        // Register our action
        Send(new ClientUpdateContextMessage
        {
            SessionId = SessionId,
            ContextKey = ContextKey,
            CharacterFunctions = actions.Select(a => a.Action).ToArray()
        });
        
        // Forward speech
        HandleMessage<ServerReplyChunkMessage>(message =>
        {
            if(!options.Value.EnableSpeech) return;

            var cleanText = textToSpeechPreprocessor.Preprocess(message.Text);
            var url = string.Format(options.Value.SpeechUrlTemplate, HttpUtility.UrlEncode(cleanText));
            logger.LogInformation("Sending speech \"{Value}\": {Url}", cleanText, url);
            _ = _httpClient.GetAsync(url).ContinueWith(HandleHttpErrors);
        });
        
        // Act when an action is called
        HandleMessage<ServerActionMessage>(message =>
        {
            if(!options.Value.EnableGestures) return;
            if (message.ContextKey != ContextKey) return;
            if (message.Role != ChatMessageRole.Assistant) return;
            
            var action = actions.FirstOrDefault(a => a.Action.Name == message.Value);
            if (action == null)
            {
                logger.LogWarning("Action {Action} not found", message.Value);
                return;
            }
            if(action.Gestures.Length == 0)
            {
                logger.LogWarning("Action {Action} has no gestures", message.Value);
                return;
            }
            var gesture = action.Gestures[Random.Shared.Next(action.Gestures.Length)];
            var url = string.Format(options.Value.GestureUrlTemplate, HttpUtility.UrlEncode(gesture.Name));
            logger.LogInformation("Playing gesture {Gesture} from action {Action}: {Url}", gesture.Name, message.Value, url);
            _ = _httpClient.GetAsync(url).ContinueWith(HandleHttpErrors);
        });
    }

    private async Task HandleHttpErrors(Task<HttpResponseMessage> t)
    {
        if (t.Exception != null)
        {
            logger.LogError("Request failed: {Exception}", t.Exception.InnerException ?? t.Exception);
            return;
        }

        if (!t.Result.IsSuccessStatusCode)
        {
            var content = await t.Result.Content.ReadAsStringAsync();
            logger.LogError("Request failed: {StatusCode}: {Content}", t.Result.StatusCode, content);
        }
    }
}