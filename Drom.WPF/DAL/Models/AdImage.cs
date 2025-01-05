using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Drom.WPF.DAL.Models;

public class AdImage
{
    public required Guid Id { get; init; }

    public required Guid AdId { get; init; }
    
    public required byte[] Bytes { get; init; }
    
    public required bool IsMain { get; set; }
}

public class AdImageConfiguration : IEntityTypeConfiguration<AdImage>
{
    public void Configure(EntityTypeBuilder<AdImage> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.AdId, x.IsMain })
            .IsUnique()
            .HasFilter($"\"{nameof(AdImage.IsMain)}\" = true");
    }
}