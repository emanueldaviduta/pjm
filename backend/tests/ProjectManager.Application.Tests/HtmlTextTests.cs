using ProjectManager.Infrastructure.Services;

namespace ProjectManager.Application.Tests;

public class HtmlTextTests
{
    [Fact]
    public void ToPlainText_KeepsTextAndLines_DropsTagsScriptsAndStyles()
    {
        const string html = """
            <html><head><style>p { color: red }</style><script>alert('x')</script></head>
            <body><p>Disk&nbsp;full on <b>db-1</b> &amp; db-2</p><div>Second   line</div><!-- hidden --></body></html>
            """;

        Assert.Equal("Disk full on db-1 & db-2\n\nSecond line", HtmlText.ToPlainText(html));
    }
}
