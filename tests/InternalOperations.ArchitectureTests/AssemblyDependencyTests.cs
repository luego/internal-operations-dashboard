using InternalOperations.Application;
using InternalOperations.Domain;
using NetArchTest.Rules;

namespace InternalOperations.ArchitectureTests;

public sealed class AssemblyDependencyTests
{
    private static readonly string[] DomainForbiddenDependencies =
    [
        "InternalOperations.Api",
        "InternalOperations.Application",
        "InternalOperations.Infrastructure",
        "InternalOperations.Persistence",
        "AutoMapper",
        "MediatR",
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
    ];

    private static readonly string[] ApplicationForbiddenDependencies =
    [
        "InternalOperations.Api",
        "InternalOperations.Infrastructure",
        "InternalOperations.Persistence",
        "Microsoft.AspNetCore",
        "Microsoft.EntityFrameworkCore",
    ];

    [Fact]
    public void DomainDoesNotDependOnOuterLayersOrFrameworks()
    {
        var result = Types.InAssembly(typeof(DomainAssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(DomainForbiddenDependencies)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void ApplicationDoesNotDependOnAdaptersOrApi()
    {
        var result = Types.InAssembly(typeof(ApplicationAssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationForbiddenDependencies)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }
}
