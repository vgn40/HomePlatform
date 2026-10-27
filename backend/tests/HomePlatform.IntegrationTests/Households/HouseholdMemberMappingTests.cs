using HomePlatform.Domain.Household;
using HomePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HomePlatform.IntegrationTests.Households;

public sealed class HouseholdMemberMappingTests
{
    [Fact]
    public void Explicit_household_id_maps_to_existing_required_column_relationship_and_unique_index()
    {
        using var context = CreateContext();
        var member = context.Model.FindEntityType(typeof(HouseholdMember))!;
        var property = member.FindProperty(nameof(HouseholdMember.HouseholdId))!;
        var table = StoreObjectIdentifier.Table("HouseholdMember", null);

        Assert.False(property.IsShadowProperty());
        Assert.Equal(typeof(HouseholdMember).GetProperty(nameof(HouseholdMember.HouseholdId)), property.PropertyInfo);
        Assert.False(property.IsNullable);
        Assert.Equal("HouseholdId", property.GetColumnName(table));
        Assert.Equal("uuid", property.GetColumnType());
        var foreignKey = Assert.Single(member.GetForeignKeys(),
            key => key.PrincipalEntityType.ClrType == typeof(Household));
        Assert.Same(property, Assert.Single(foreignKey.Properties));
        Assert.True(foreignKey.IsRequired);
        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
        var index = Assert.Single(member.GetIndexes(), candidate => candidate.IsUnique);
        Assert.Equal(new[] { nameof(HouseholdMember.HouseholdId), nameof(HouseholdMember.PersonId) },
            index.Properties.Select(candidate => candidate.Name));
        Assert.DoesNotContain(member.GetProperties(), candidate => candidate.IsShadowProperty());
    }

    [Fact]
    public void Explicit_household_id_has_no_schema_difference_from_existing_migration_snapshot()
    {
        using var context = CreateContext();

        Assert.False(context.Database.HasPendingModelChanges());
    }

    private static HomePlatformDbContext CreateContext() => new(
        new DbContextOptionsBuilder<HomePlatformDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only")
            .Options);
}
