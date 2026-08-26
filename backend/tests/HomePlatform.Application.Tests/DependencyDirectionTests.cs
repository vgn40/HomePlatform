using System.Reflection;

namespace HomePlatform.Application.Tests;

public sealed class DependencyDirectionTests
{
    [Fact]
    public void Application_does_not_reference_outer_layers()
    {
        var references = Assembly.Load("HomePlatform.Application")
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.DoesNotContain("HomePlatform.Infrastructure", references);
        Assert.DoesNotContain("HomePlatform.Api", references);
    }
}
