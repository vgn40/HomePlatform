using HomePlatform.Domain.Household;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomePlatform.Infrastructure.Persistence.Configurations;

public sealed class HouseholdConfiguration
    : IEntityTypeConfiguration<Household>
{
    public void Configure(EntityTypeBuilder<Household> builder)
    {
        builder.HasKey(household => household.Id);

        builder.Property(household => household.Id)
            .ValueGeneratedNever();

        builder.Property(household => household.Name)
            .IsRequired()
            .HasMaxLength(Household.MaxNameLength);

        builder.Property(household => household.CreatedAt)
            .IsRequired();

        builder.Property(household => household.UpdatedAt)
            .IsRequired();

        builder.HasMany(household => household.Members)
            .WithOne()
            .HasForeignKey("HouseholdId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(household => household.Members)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
