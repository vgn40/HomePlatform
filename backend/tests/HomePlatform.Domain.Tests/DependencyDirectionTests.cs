using System.Reflection;

namespace HomePlatform.Domain.Tests;

public sealed class DependencyDirectionTests
{
    [Fact]
    public void Domain_does_not_reference_outer_layers()
    {
        var references = Assembly.Load("HomePlatform.Domain")
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.DoesNotContain("HomePlatform.Application", references);
        Assert.DoesNotContain("HomePlatform.Infrastructure", references);
        Assert.DoesNotContain("HomePlatform.Api", references);
    }
}
