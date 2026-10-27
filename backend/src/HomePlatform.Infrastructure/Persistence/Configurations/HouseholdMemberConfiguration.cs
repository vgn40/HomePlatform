using HomePlatform.Domain.Household;
using HomePlatform.Domain.Person;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomePlatform.Infrastructure.Persistence.Configurations;

public sealed class HouseholdMemberConfiguration
    : IEntityTypeConfiguration<HouseholdMember>
{
    public void Configure(
        EntityTypeBuilder<HouseholdMember> builder)
    {
        builder.HasKey(member => member.MembershipId);

        builder.Property(member => member.MembershipId)
            .ValueGeneratedNever();

        builder.Property(member => member.PersonId)
            .IsRequired();

        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(member => member.PersonId)
            .OnDelete(DeleteBehavior.ClientNoAction);

        builder.Property(member => member.Role)
            .IsRequired();

        builder.HasIndex(member => new { member.HouseholdId, member.PersonId })
            .IsUnique();
    }
}
