using HomePlatform.Domain.Person;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomePlatform.Infrastructure.Persistence.Configurations;

public sealed class PersonConfiguration
    : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.HasKey(person => person.Id);

        builder.Property(person => person.Id)
            .ValueGeneratedNever();

        builder.Property(person => person.DisplayName)
            .IsRequired(false);

    }
}
