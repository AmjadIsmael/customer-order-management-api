namespace CustomerOrderManagement.Domain.DTOs.Products;

/// <summary>
/// The details required to create a new product.
/// </summary>
public sealed class ProductCreateDto
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }
}
