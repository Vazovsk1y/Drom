using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClothesStore.WPF.DAL.Models;

public class Product
{
    public required Guid Id { get; init; }
    
    public required string Title { get; set; }
    
    public required string Description { get; set; }
    
    public required decimal Price { get; set; }
    
    public required ProductCategory Category { get; set; }
    
    public IEnumerable<ProductImage> Images { get; init; } = new List<ProductImage>();
    
    public IEnumerable<SizeOption> AvailableSizes { get; init; } = new List<SizeOption>();
}

public enum ProductCategory
{
    [Display(Name = "Верхняя одежда")]
    Outerwear,

    [Display(Name = "Худи и толстовки")]
    HoodiesAndSweatshirts,

    [Display(Name = "Футболки")]
    TShirts,

    [Display(Name = "Лонгсливы")]
    LongSleeves,

    [Display(Name = "Шорты")]
    Shorts,

    [Display(Name = "Штаны")]
    Pants,

    [Display(Name = "Спортивные костюмы")]
    Tracksuits,

    [Display(Name = "Аксессуары")]
    Accessories
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.HasMany(x => x.Images)
            .WithOne()
            .HasForeignKey(e => e.ProductId);
        
        builder.Property(e => e.Category)
            .HasConversion(e => e.ToString(), e => Enum.Parse<ProductCategory>(e));
        
        builder.HasMany(x => x.AvailableSizes)
            .WithOne(e => e.Product)
            .HasForeignKey(e => e.ProductId);
    }
}