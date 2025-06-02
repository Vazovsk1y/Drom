using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClothesStore.WPF.DAL.Models;

public class User
{
    public required Guid Id { get; init; }

    public required string Username { get; init; }
    
    public required string PhoneNumber { get; init; }
    
    public required string PasswordHash { get; init; }
    
    public required Role Role { get; init; }
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
        
        builder
            .Property(e => e.Role)
            .HasConversion(e => e.ToString(), e => Enum.Parse<Role>(e));
    }
}

public enum Role
{
    Admin,
    User,
}