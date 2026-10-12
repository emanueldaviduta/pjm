using System.Text;
using System.Text.RegularExpressions;
using MimeKit.Text;

namespace ProjectManager.Infrastructure.Services;

/// <summary>Turns an HTML email body into plain text: keeps the text, drops tags, scripts and styles, breaks lines at blocks.</summary>
public static partial class HtmlText
{
    private static readonly HashSet<string> LineBreaks = new(StringComparer.OrdinalIgnoreCase)
    {
        "br", "p", "div", "li", "tr", "table", "blockquote", "h1", "h2", "h3", "h4", "h5", "h6", "hr", "pre"
    };

    public static string ToPlainText(string html)
    {
        var text = new StringBuilder();
        var tokenizer = new HtmlTokenizer(new StringReader(html)) { DecodeCharacterReferences = true };
        var skipping = false;

        while (tokenizer.ReadNextToken(out var token))
        {
            switch (token)
            {
                case HtmlTagToken tag when tag.Name is "script" or "style":
                    skipping = !tag.IsEndTag;
                    break;
                case HtmlTagToken tag when LineBreaks.Contains(tag.Name):
                    text.Append('\n');
                    break;
                case HtmlTagToken { Name: "td" or "th", IsEndTag: true }:
                    text.Append('\t');
                    break;
                case HtmlCommentToken:
                    break;
                case HtmlDataToken data when !skipping:
                    text.Append(data.Data);
                    break;
            }
        }

        var collapsed = Spaces().Replace(text.ToString(), " ");
        collapsed = string.Join('\n', collapsed.Split('\n').Select(l => l.Trim()));
        return BlankLines().Replace(collapsed, "\n\n").Trim();
    }

    [GeneratedRegex(@"[ \t\r ]+")]
    private static partial Regex Spaces();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex BlankLines();
}
