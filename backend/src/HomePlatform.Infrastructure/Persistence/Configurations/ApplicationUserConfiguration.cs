using HomePlatform.Domain.Person;
using HomePlatform.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomePlatform.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.HasOne<Person>().WithOne()
            .HasForeignKey<ApplicationUser>(user => user.PersonId)
            .IsRequired()
            .OnDelete(DeleteBehavior.ClientNoAction);
    }
}
