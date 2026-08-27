using CustomerOrderManagement.Domain.DTOs.Products;
using CustomerOrderManagement.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace CustomerOrderManagement.Business.Mapping;

[Mapper]
public static partial class ProductMappingExtensions
{
    [MapperIgnoreTarget(nameof(Product.Id))]
    [MapperIgnoreTarget(nameof(Product.CreatedDate))]
    [MapperIgnoreTarget(nameof(Product.CreatedBy))]
    [MapperIgnoreTarget(nameof(Product.UpdatedDate))]
    [MapperIgnoreTarget(nameof(Product.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Product.IsActive))]
    [MapperIgnoreTarget(nameof(Product.IsDeleted))]
    [MapperIgnoreTarget(nameof(Product.OrderItems))]
    public static partial Product ToEntity(this ProductCreateDto dto);

    [MapperIgnoreTarget(nameof(Product.Id))]
    [MapperIgnoreTarget(nameof(Product.CreatedDate))]
    [MapperIgnoreTarget(nameof(Product.CreatedBy))]
    [MapperIgnoreTarget(nameof(Product.UpdatedDate))]
    [MapperIgnoreTarget(nameof(Product.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Product.IsActive))]
    [MapperIgnoreTarget(nameof(Product.IsDeleted))]
    [MapperIgnoreTarget(nameof(Product.OrderItems))]
    public static partial void ApplyTo(this ProductUpdateDto dto, Product product);

    [MapperIgnoreSource(nameof(Product.IsDeleted))]
    [MapperIgnoreSource(nameof(Product.OrderItems))]
    public static partial ProductResponseDto ToResponseDto(this Product product);
}
