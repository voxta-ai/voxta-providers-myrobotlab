using System.Text.RegularExpressions;
using System.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Voxta.Model.Shared;
using Voxta.Model.WebsocketMessages.ClientMessages;
using Voxta.Model.WebsocketMessages.ServerMessages;
using Voxta.Providers.Host;

namespace Voxta.Providers.MyRobotLab.Providers;

[UsedImplicitly]
public partial class MyRobotLabProvider(
    IRemoteChatSession session,
    IHttpClientFactory httpClientFactory,
    IVoxtaActionsYamlRepository repository,
    IOptions<MyRobotLabOptions> options,
    ILogger<MyRobotLabProvider> logger
) : ProviderBase(session, logger)
{
    private const string ContextKey = "MyRobotLab";
    
    [GeneratedRegex(@"^[a-zA-Z.,;!?0-9 ]", RegexOptions.Compiled)]
    private static partial Regex CleanTextForSpeechRegex();
    
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
            CharacterFunctions = actions
        });
        
        // Forward speech
        HandleMessage<ServerReplyChunkMessage>(message =>
        {
            if(!options.Value.EnableSpeech) return;

            var cleanText = CleanTextForSpeechRegex().Replace(message.Text, "");
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
            
            var url = string.Format(options.Value.GestureUrlTemplate, HttpUtility.UrlEncode(message.Value));
            logger.LogInformation("Playing gesture {Gesture}: {Url}", message.Value, url);
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