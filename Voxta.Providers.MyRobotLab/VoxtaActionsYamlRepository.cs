using Voxta.Model.Shared;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Voxta.Providers.MyRobotLab.Providers;

public interface IVoxtaActionsYamlRepository
{
    Task<FunctionDefinition[]> DeserializeAsync();
}

public class VoxtaActionsYamlRepository : IVoxtaActionsYamlRepository
{
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();
    
    public async Task<FunctionDefinition[]> DeserializeAsync()
    {
        var yaml = await File.ReadAllTextAsync("actions.yml");
        return _deserializer.Deserialize<FunctionDefinition[]>(yaml);
    }
}
