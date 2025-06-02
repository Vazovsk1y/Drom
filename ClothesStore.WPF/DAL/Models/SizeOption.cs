namespace ClothesStore.WPF.DAL.Models;

public class SizeOption
{
    public required Guid Id { get; init; }

    public required ClothingSize Size { get; init; }

    public required int QuantityInStock { get; init; }

    public required Guid ProductId { get; set; }

    public Product Product { get; init; } = null!;
}

public enum ClothingSize
{
    XS,
    S,
    M,
    L,
    XL,
    XXL,
    XXXL
}