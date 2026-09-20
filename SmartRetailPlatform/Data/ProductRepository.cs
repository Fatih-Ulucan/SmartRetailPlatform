using Npgsql;
using SmartRetailPlatform.Models;

namespace SmartRetailPlatform.Data;

public class ProductRepository
{
    public async Task<Product?> GetByIdAsync(int id)
    {
        await using var connection = new NpgsqlConnection(DbConfig.ConnectionString);
        await connection.OpenAsync();

        const string sql = "SELECT id, name, price, stock FROM products WHERE id = @id";

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            string name = reader.GetString(1);
            decimal price = reader.GetDecimal(2);
            int stock = reader.GetInt32(3);

            return new Product(name, price, stock);
        }

        return null;
    }
}