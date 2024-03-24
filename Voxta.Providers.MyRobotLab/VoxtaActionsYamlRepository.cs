using Voxta.Model.Shared;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Voxta.Providers.MyRobotLab.Providers;

public interface IVoxtaActionsYamlRepository
{
    Task<IntegratedAction[]> DeserializeAsync();
}

public class VoxtaActionsYamlRepository : IVoxtaActionsYamlRepository
{
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();
    
    public async Task<IntegratedAction[]> DeserializeAsync()
    {
        var yaml = await File.ReadAllTextAsync("actions.yml");
        return _deserializer.Deserialize<IntegratedAction[]>(yaml);
    }
}

[Serializable]
public class IntegratedAction
{
    public required FunctionDefinition Action { get; set; }
    public MyRobotLabGesture[] Gestures { get; set; } = Array.Empty<MyRobotLabGesture>();
}

[Serializable]
public class MyRobotLabGesture
{
    public required string Name { get; set; }
    public required float Probability { get; set; }
}
