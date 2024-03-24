using System.Text.RegularExpressions;

namespace Voxta.Providers.MyRobotLab;

public interface ITextToSpeechPreprocessor
{
    string Preprocess(string text);
}

public partial class TextToSpeechPreprocessor : ITextToSpeechPreprocessor
{
    [GeneratedRegex(@"\*[^*]+\*?", RegexOptions.Compiled)]
    private static partial Regex RemoveAllNonChat();
    [GeneratedRegex(@"\[[^\]]+\] ?", RegexOptions.Compiled)]
    private static partial Regex RemoveNotes();
    [GeneratedRegex(@"(?<=^|\s)\>?[:;X]-?[)(|P](?=$|\s)", RegexOptions.Compiled)]
    private static partial Regex Smileys();
    [GeneratedRegex("[~]+", RegexOptions.Compiled)]
    private static partial Regex Decorations();

    public string Preprocess(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text == "...") return "...";

        var output = text;
        
        // Smileys and role-play
        output = Smileys().Replace(output, "");
        output = RemoveNotes().Replace(output, "");
        output = RemoveAllNonChat().Replace(output, "");
        output = Decorations().Replace(output, "...");

        // Special characters
        output = output.Replace(";", ",");
        output = output.Replace("–", "...");
        output = output.Replace(" - ", "... ");

        // Trim, avoid empty strings
        output = output.Trim();
        if (output == "") output = "...";

        return output;
    }
}