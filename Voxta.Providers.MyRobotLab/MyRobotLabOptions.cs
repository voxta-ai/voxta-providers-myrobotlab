using System.ComponentModel.DataAnnotations;

namespace Voxta.Providers.MyRobotLab;

[Serializable]
public class MyRobotLabOptions
{
    [Required]
    public required bool EnableGestures { get; init; }
    
    [Required]
    // [RegularExpression(@"^https?:\/\/.+\{0\}")]
    public required string GestureUrlTemplate { get; init; }
    
    [Required]
    public required bool EnableSpeech { get; init; }
    
    [Required]
    // [RegularExpression(@"^https?:\/\/.+\{0\}")]
    public required string SpeechUrlTemplate { get; init; }
}
