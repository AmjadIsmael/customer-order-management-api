using CustomerOrderManagement.Domain.DTOs.Customers;
using CustomerOrderManagement.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace CustomerOrderManagement.Business.Mapping;

[Mapper]
public static partial class CustomerMappingExtensions
{
    [MapperIgnoreTarget(nameof(Customer.Id))]
    [MapperIgnoreTarget(nameof(Customer.CreatedDate))]
    [MapperIgnoreTarget(nameof(Customer.CreatedBy))]
    [MapperIgnoreTarget(nameof(Customer.UpdatedDate))]
    [MapperIgnoreTarget(nameof(Customer.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Customer.IsActive))]
    [MapperIgnoreTarget(nameof(Customer.IsDeleted))]
    public static partial Customer ToEntity(this CustomerCreateDto dto);

    [MapperIgnoreTarget(nameof(Customer.Id))]
    [MapperIgnoreTarget(nameof(Customer.CreatedDate))]
    [MapperIgnoreTarget(nameof(Customer.CreatedBy))]
    [MapperIgnoreTarget(nameof(Customer.UpdatedDate))]
    [MapperIgnoreTarget(nameof(Customer.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Customer.IsActive))]
    [MapperIgnoreTarget(nameof(Customer.IsDeleted))]
    public static partial void ApplyTo(this CustomerUpdateDto dto, Customer customer);

    [MapperIgnoreSource(nameof(Customer.IsDeleted))]
    public static partial CustomerResponseDto ToResponseDto(this Customer customer);
}
