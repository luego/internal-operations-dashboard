using System.Xml.Linq;

namespace InternalOperations.ArchitectureTests;

public sealed class ProjectDependencyTests
{
    private static readonly IReadOnlyDictionary<string, string[]> ExpectedReferences =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["src/InternalOperations.Domain/InternalOperations.Domain.csproj"] = [],
            ["src/InternalOperations.Shared/InternalOperations.Shared.csproj"] = [],
            ["src/InternalOperations.Application/InternalOperations.Application.csproj"] =
            [
                "src/InternalOperations.Domain/InternalOperations.Domain.csproj",
                "src/InternalOperations.Shared/InternalOperations.Shared.csproj",
            ],
            ["src/InternalOperations.Infrastructure/InternalOperations.Infrastructure.csproj"] =
            [
                "src/InternalOperations.Application/InternalOperations.Application.csproj",
                "src/InternalOperations.Shared/InternalOperations.Shared.csproj",
            ],
            ["src/InternalOperations.Persistence/InternalOperations.Persistence.csproj"] =
            [
                "src/InternalOperations.Application/InternalOperations.Application.csproj",
                "src/InternalOperations.Domain/InternalOperations.Domain.csproj",
                "src/InternalOperations.Shared/InternalOperations.Shared.csproj",
            ],
            ["src/InternalOperations.Persistence.Migrations.PostgreSql/InternalOperations.Persistence.Migrations.PostgreSql.csproj"] =
            [
                "src/InternalOperations.Persistence/InternalOperations.Persistence.csproj",
            ],
            ["src/InternalOperations.Persistence.Migrations.SqlServer/InternalOperations.Persistence.Migrations.SqlServer.csproj"] =
            [
                "src/InternalOperations.Persistence/InternalOperations.Persistence.csproj",
            ],
            ["src/InternalOperations.Api/InternalOperations.Api.csproj"] =
            [
                "src/InternalOperations.Application/InternalOperations.Application.csproj",
                "src/InternalOperations.Infrastructure/InternalOperations.Infrastructure.csproj",
                "src/InternalOperations.Persistence/InternalOperations.Persistence.csproj",
                "src/InternalOperations.Persistence.Migrations.PostgreSql/InternalOperations.Persistence.Migrations.PostgreSql.csproj",
                "src/InternalOperations.Persistence.Migrations.SqlServer/InternalOperations.Persistence.Migrations.SqlServer.csproj",
                "src/InternalOperations.Shared/InternalOperations.Shared.csproj",
            ],
        };

    public static TheoryData<string> ProductProjects => new(ExpectedReferences.Keys);

    [Theory]
    [MemberData(nameof(ProductProjects))]
    public void ProductProjectHasOnlyAllowedProjectReferences(string projectPath)
    {
        var repositoryRoot = FindRepositoryRoot();
        var absoluteProjectPath = Path.Combine(repositoryRoot.FullName, projectPath);
        var projectDirectory = Path.GetDirectoryName(absoluteProjectPath)!;
        var document = XDocument.Load(absoluteProjectPath);

        var actualReferences = document
            .Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(path => path is not null)
            .Select(path => path!.Replace('\\', Path.DirectorySeparatorChar))
            .Select(path => Path.GetFullPath(path, projectDirectory))
            .Select(path => Path.GetRelativePath(repositoryRoot.FullName, path).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .ToArray();

        var expectedReferences = ExpectedReferences[projectPath]
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedReferences, actualReferences);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "InternalOperations.slnx")))
            {
                return current;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
