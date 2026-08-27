namespace CustomerOrderManagement.Domain.DTOs.Products;

/// <summary>
/// The full set of editable fields for an existing product.
/// </summary>
public sealed class ProductUpdateDto
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }
}
