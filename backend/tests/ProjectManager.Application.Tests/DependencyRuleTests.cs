using System.Xml.Linq;

namespace ProjectManager.Application.Tests;

/// <summary>Fails when a project file breaks the layering rule of the backend-architecture spec.</summary>
public class DependencyRuleTests
{
    private static readonly string SrcDir = FindSrcDir();

    private static string FindSrcDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "PJMSolution.slnx")))
            dir = dir.Parent;
        return dir == null
            ? throw new InvalidOperationException("PJMSolution.slnx not found above the test output folder.")
            : Path.Combine(dir.FullName, "src");
    }

    private static XDocument Load(string project) =>
        XDocument.Load(Path.Combine(SrcDir, $"ProjectManager.{project}", $"ProjectManager.{project}.csproj"));

    private static string[] ProjectReferences(string project) =>
        Load(project).Descendants("ProjectReference")
            .Select(e => Path.GetFileNameWithoutExtension(((string)e.Attribute("Include")!).Split((char)47, (char)92).Last()))
            .OrderBy(n => n).ToArray();

    private static string[] PackageReferences(string project) =>
        Load(project).Descendants("PackageReference").Select(e => (string)e.Attribute("Include")!).ToArray();

    [Fact]
    public void Domain_HasNoReferences()
    {
        Assert.Empty(ProjectReferences("Domain"));
        Assert.Empty(PackageReferences("Domain"));
        Assert.Empty(Load("Domain").Descendants("FrameworkReference"));
    }

    [Fact]
    public void Application_ReferencesOnlyDomain_AndNoProviderJwtOrHostingPackage()
    {
        Assert.Equal(["ProjectManager.Domain"], ProjectReferences("Application"));

        string[] forbidden =
        [
            "Npgsql", "Microsoft.EntityFrameworkCore.Sqlite", "Microsoft.EntityFrameworkCore.SqlServer",
            "Microsoft.EntityFrameworkCore.Design", "System.IdentityModel.Tokens.Jwt", "Microsoft.IdentityModel",
            "Microsoft.AspNetCore", "Microsoft.Extensions.Hosting"
        ];
        var offending = PackageReferences("Application")
            .Where(p => forbidden.Any(f => p.StartsWith(f, StringComparison.OrdinalIgnoreCase))).ToArray();
        Assert.Empty(offending);

        var doc = Load("Application");
        Assert.Equal("Microsoft.NET.Sdk", (string?)doc.Root!.Attribute("Sdk"));
        Assert.Empty(doc.Descendants("FrameworkReference"));
    }

    [Fact]
    public void Infrastructure_ReferencesOnlyApplication() =>
        Assert.Equal(["ProjectManager.Application"], ProjectReferences("Infrastructure"));

    [Fact]
    public void Api_ReferencesOnlyApplicationAndInfrastructure() =>
        Assert.Equal(["ProjectManager.Application", "ProjectManager.Infrastructure"], ProjectReferences("Api"));
}
