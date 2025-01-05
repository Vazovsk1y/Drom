using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Drom.WPF.DAL.Models;

public class FavoriteAd
{
    public required Guid AdId { get; init; }
    
    public required Guid UserId { get; init; }
    
    public Ad Ad { get; init; } = null!;
    
    public User User { get; init; } = null!;
}

public class FavoriteAdConfiguration : IEntityTypeConfiguration<FavoriteAd>
{
    public void Configure(EntityTypeBuilder<FavoriteAd> builder)
    {
        builder.HasKey(x => new { x.AdId, x.UserId });

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId);
        
        builder.HasOne(e => e.Ad)
            .WithMany()
            .HasForeignKey(e => e.AdId);
    }
}