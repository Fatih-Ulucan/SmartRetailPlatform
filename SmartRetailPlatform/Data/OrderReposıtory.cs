using Npgsql;
using SmartRetailPlatform.Data;
using SmartRetailPlatform.Models;

namespace SmartRetailPlatform.Data;

public class OrderRepository
{
    public async Task<int> CreateOrderAsync(int customerId)
    {
        await using var connection = new NpgsqlConnection(DbConfig.ConnectionString);
        await connection.OpenAsync();

        const string sql = "INSERT INTO orders (customer_id) VALUES (@customerId) RETURNING id";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("customerId", customerId);

        var result = await cmd.ExecuteScalarAsync();
        return (int)result!;
    }

    public async Task AddOrderItemAsync(int orderId, int productId, int quantity, decimal unitPrice)
    {
        await using var connection = new NpgsqlConnection(DbConfig.ConnectionString);
        await connection.OpenAsync();

        const string sql = @"
            INSERT INTO order_items (order_id, product_id, quantity, unit_price)
            VALUES (@orderId, @productId, @quantity, @unitPrice);";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("orderId", orderId);
        cmd.Parameters.AddWithValue("productId", productId);
        cmd.Parameters.AddWithValue("quantity", quantity);
        cmd.Parameters.AddWithValue("unitPrice", unitPrice);

        await cmd.ExecuteNonQueryAsync();
    }
    public async Task<OrderDetailsResponse?> GetOrderDetailsAsync(int orderId)
    {
        await using var connection = new NpgsqlConnection(DbConfig.ConnectionString);
        await connection.OpenAsync();

        const string sql = @"
        SELECT
            o.id AS order_id,
            c.name AS customer_name,
            c.customer_type,
            p.name AS product_name,
            oi.quantity,
            oi.unit_price
        FROM orders o
        JOIN customers c ON o.customer_id = c.id
        JOIN order_items oi ON oi.order_id = o.id
        JOIN products p ON oi.product_id = p.id
        WHERE o.id = @orderId";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("orderId", orderId);

        await using var reader = await cmd.ExecuteReaderAsync();

        OrderDetailsResponse? result = null;

        while (await reader.ReadAsync())
        {
            if (result == null)
            {
                result = new OrderDetailsResponse
                {
                    OrderId = reader.GetInt32(0),
                    CustomerName = reader.GetString(1),
                    CustomerType = reader.GetString(2)
                };
            }

            result.Items.Add(new OrderItemResponse
            {
                ProductName = reader.GetString(3),
                Quantity = reader.GetInt32(4),
                UnitPrice = reader.GetDecimal(5)
            });
        }

        return result;
    }
}