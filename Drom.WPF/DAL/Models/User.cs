using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Drom.WPF.DAL.Models;

public class User
{
    public Guid Id { get; } = Guid.NewGuid();

    public required string Username { get; init; }
    
    public required string PhoneNumber { get; init; }
    
    public required string PasswordHash { get; init; }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder
            .HasKey(x => x.Id);
        
        builder
            .HasIndex(x => x.Username)
            .IsUnique();
        
        builder
            .HasIndex(e => e.PhoneNumber)
            .IsUnique();

        builder
            .Property(x => x.Username)
            .UseCollation(DromDbContext.CaseInsensitiveCollation);
    }
}