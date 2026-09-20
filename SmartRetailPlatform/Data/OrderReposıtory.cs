using Npgsql;
using SmartRetailPlatform.Data;

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
}