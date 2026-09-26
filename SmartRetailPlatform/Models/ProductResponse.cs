using System.Data.Common;

namespace SmartRetailPlatform.Models;

public class ProductResponse
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }

    public static ProductResponse FromProduct(Product product)
    {
        return new ProductResponse
        {
            Name = product.Name,
            Price = product.Price,
            Stock = product.GetStock()
        };
    }
}