namespace Codify.Infrastructure.AI;

public static class PromptTemplate
{
    public static string Render(string template, IReadOnlyDictionary<string, string> values)
    {
        var result = template;
        foreach (var pair in values)
        {
            var token = "{{" + pair.Key + "}}";
            result = result.Replace(token, pair.Value ?? string.Empty, StringComparison.Ordinal);
        }

        return result;
    }
}
