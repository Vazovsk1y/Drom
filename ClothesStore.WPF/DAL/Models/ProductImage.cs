using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClothesStore.WPF.DAL.Models;

public class ProductImage
{
    public required Guid Id { get; init; }

    public required Guid ProductId { get; init; }
    
    public required byte[] Bytes { get; init; }
    
    public required bool IsMain { get; set; }
}

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { AdId = x.ProductId, x.IsMain })
            .IsUnique()
            .HasFilter($"\"{nameof(ProductImage.IsMain)}\" = true");
    }
}