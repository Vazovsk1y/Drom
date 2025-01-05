using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Drom.WPF.DAL.Models;

public class Ad
{
    public required Guid Id { get; init; }
    
    public required Guid UserId { get; init; }
    
    public required DateTimeOffset CreationDateTime { get; init; }
    
    public required string CarModelName { get; set; }
    
    public required string CarBrandName { get; set; }
    
    public required int CarYear { get; set; }
    
    public required string Description { get; set; }
    
    public required decimal Price { get; set; }
    
    public User User { get; init; } = null!;
    
    public IEnumerable<AdImage> AdImages { get; init; } = new List<AdImage>();
}

public class AdConfiguration : IEntityTypeConfiguration<Ad>
{
    public void Configure(EntityTypeBuilder<Ad> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasMany(x => x.AdImages)
            .WithOne()
            .HasForeignKey(e => e.AdId);

        builder
            .HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId);
    }
}