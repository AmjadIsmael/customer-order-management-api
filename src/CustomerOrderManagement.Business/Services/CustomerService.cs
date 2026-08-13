using CustomerOrderManagement.Business.Exceptions;
using CustomerOrderManagement.Business.Interfaces.Persistence;
using CustomerOrderManagement.Business.Interfaces.Services;
using CustomerOrderManagement.Business.Mapping;
using CustomerOrderManagement.Domain.DTOs.Customers;

namespace CustomerOrderManagement.Business.Services;

public sealed class CustomerService : ICustomerService
{
    private const string SystemUser = "system";

    private readonly IUnitOfWork _unitOfWork;

    public CustomerService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerResponseDto> CreateAsync(
        CustomerCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        if (await _unitOfWork.Customers.EmailExistsAsync(
                dto.Email,
                cancellationToken: cancellationToken))
        {
            throw new ConflictException(
                $"A customer with email '{dto.Email}' already exists.");
        }

        var customer = dto.ToEntity();
        customer.CreatedDate = DateTime.UtcNow;
        customer.CreatedBy = SystemUser;

        await _unitOfWork.Customers.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return customer.ToResponseDto();
    }

    public async Task<CustomerResponseDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Customer '{id}' was not found.");

        return customer.ToResponseDto();
    }

    public async Task<IReadOnlyList<CustomerResponseDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var customers = await _unitOfWork.Customers.GetAllAsync(cancellationToken);

        return customers
            .Select(customer => customer.ToResponseDto())
            .ToList();
    }

    public async Task<CustomerResponseDto> UpdateAsync(
        Guid id,
        CustomerUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Customer '{id}' was not found.");

        if (await _unitOfWork.Customers.EmailExistsAsync(
                dto.Email,
                id,
                cancellationToken))
        {
            throw new ConflictException(
                $"A customer with email '{dto.Email}' already exists.");
        }

        dto.ApplyTo(customer);
        customer.UpdatedDate = DateTime.UtcNow;
        customer.UpdatedBy = SystemUser;

        _unitOfWork.Customers.Update(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return customer.ToResponseDto();
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Customer '{id}' was not found.");

        customer.IsDeleted = true;
        customer.IsActive = false;
        customer.UpdatedDate = DateTime.UtcNow;
        customer.UpdatedBy = SystemUser;

        _unitOfWork.Customers.Update(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
