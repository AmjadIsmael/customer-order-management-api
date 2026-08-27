using System.Data;
using CustomerOrderManagement.Business.Interfaces.Persistence;
using CustomerOrderManagement.Domain.DTOs.Orders;
using CustomerOrderManagement.Domain.Entities;
using CustomerOrderManagement.Domain.Enums;
using CustomerOrderManagement.Persistence.Context;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace CustomerOrderManagement.Persistence.Repositories;

public sealed class OrderRepository
    : Repository<Order>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public override async Task<Order?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await Entities
            .Include(order => order.Items.Where(item => !item.IsDeleted))
            .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(
                order => order.Id == id && !order.IsDeleted,
                cancellationToken);
    }

    public override async Task<IReadOnlyList<Order>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await Entities
            .AsNoTracking()
            .Include(order => order.Items.Where(item => !item.IsDeleted))
            .ThenInclude(item => item.Product)
            .Where(order => !order.IsDeleted)
            .ToListAsync(cancellationToken);
    }


    public async Task<CustomerOrderSummaryDto> GetCustomerOrderSummaryAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var connection = await GetOpenConnectionAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("p_customer_id", customerId, DbType.Guid);
        parameters.Add("total_orders", dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
        parameters.Add("total_spent", dbType: DbType.Decimal, direction: ParameterDirection.InputOutput);
        parameters.Add("pending_orders", dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
        parameters.Add("last_order_date", dbType: DbType.DateTime, direction: ParameterDirection.InputOutput);

        await connection.ExecuteAsync(new CommandDefinition(
            """
            CALL "GetCustomerOrderSummary"(@p_customer_id, @total_orders, @total_spent, @pending_orders, @last_order_date)
            """,
            parameters,
            cancellationToken: cancellationToken));

        return new CustomerOrderSummaryDto
        {
            CustomerId = customerId,
            TotalOrders = parameters.Get<int>("total_orders"),
            TotalSpent = parameters.Get<decimal>("total_spent"),
            PendingOrders = parameters.Get<int>("pending_orders"),
            LastOrderDate = parameters.Get<DateTime?>("last_order_date"),
        };
    }

    public async Task<IReadOnlyList<OrderSearchResultDto>> SearchOrdersAsync(
        OrderSearchDto filter,
        CancellationToken cancellationToken = default)
    {
        var connection = (NpgsqlConnection)await GetOpenConnectionAsync(cancellationToken);
        var cursorName = $"search_orders_{Guid.NewGuid():N}";

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var callCommand = connection.CreateCommand())
        {
            callCommand.Transaction = transaction;
            callCommand.CommandText = """
                CALL "SearchOrders"(@p_customer_id, @p_status, @p_from_date, @p_to_date, @p_cursor)
                """;
            callCommand.Parameters.Add(new NpgsqlParameter("p_customer_id", NpgsqlDbType.Uuid)
            {
                Value = (object?)filter.CustomerId ?? DBNull.Value,
            });
            callCommand.Parameters.Add(new NpgsqlParameter("p_status", NpgsqlDbType.Integer)
            {
                Value = (object?)(int?)filter.Status ?? DBNull.Value,
            });
            callCommand.Parameters.Add(new NpgsqlParameter("p_from_date", NpgsqlDbType.TimestampTz)
            {
                Value = (object?)filter.FromDate ?? DBNull.Value,
            });
            callCommand.Parameters.Add(new NpgsqlParameter("p_to_date", NpgsqlDbType.TimestampTz)
            {
                Value = (object?)filter.ToDate ?? DBNull.Value,
            });
            callCommand.Parameters.Add(new NpgsqlParameter("p_cursor", NpgsqlDbType.Refcursor)
            {
                Value = cursorName,
            });

            await callCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        var results = (await connection.QueryAsync<OrderSearchResultDto>(new CommandDefinition(
            $"""FETCH ALL FROM "{cursorName}" """,
            transaction: transaction,
            cancellationToken: cancellationToken))).AsList();

        await transaction.CommitAsync(cancellationToken);

        return results;
    }

    public async Task<IReadOnlyList<Order>> GetStalePendingAsync(
        DateTime cutoffUtc,
        CancellationToken cancellationToken = default)
    {
        return await Entities
            .Include(order => order.Items.Where(item => !item.IsDeleted))
            .ThenInclude(item => item.Product)
            .Where(order => !order.IsDeleted
                && order.Status == OrderStatus.Pending
                && order.CreatedDate <= cutoffUtc)
            .ToListAsync(cancellationToken);
    }

    private async Task<IDbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = Context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        return connection;
    }
}
