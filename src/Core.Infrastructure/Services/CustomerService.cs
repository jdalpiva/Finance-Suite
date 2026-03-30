using Microsoft.EntityFrameworkCore;
using SMEFinanceSuite.Core.Application.Abstractions;
using SMEFinanceSuite.Core.Application.Customers;
using SMEFinanceSuite.Core.Domain.Entities;
using SMEFinanceSuite.Core.Infrastructure.Persistence;

namespace SMEFinanceSuite.Core.Infrastructure.Services;

public sealed class CustomerService(IDbContextFactory<AppDbContext> dbContextFactory) : ICustomerService
{
    public async Task<Guid> RegisterAsync(CreateCustomerCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Customer customer = new(
            name: command.Name,
            email: command.Email,
            phone: command.Phone);

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync(cancellationToken);

        return customer.Id;
    }

    public async Task<IReadOnlyList<CustomerListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<CustomerListItemDto> customers = await dbContext.Customers
            .AsNoTracking()
            .OrderBy(customer => customer.Name)
            .ThenByDescending(customer => customer.CreatedAtUtc)
            .Take(200)
            .Select(customer => new CustomerListItemDto(
                Id: customer.Id,
                Name: customer.Name,
                Email: customer.Email,
                Phone: customer.Phone))
            .ToListAsync(cancellationToken);

        return customers;
    }

    public async Task UpdateAsync(UpdateCustomerCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Customer? customer = await dbContext.Customers
            .FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);

        if (customer is null)
        {
            throw new InvalidOperationException("Cliente informado não foi encontrado.");
        }

        customer.Rename(command.Name);
        customer.UpdateContact(command.Email, command.Phone);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Customer? customer = await dbContext.Customers
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (customer is null)
        {
            throw new InvalidOperationException("Cliente informado não foi encontrado.");
        }

        dbContext.Customers.Remove(customer);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
