namespace SmartRetailPlatform.Models;

public class OrderItem
{
    public Product Product { get; }
    public int Quantity { get; }

    public OrderItem(Product product, int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.");
        
        Product = product;
        Quantity = quantity;
        
    }
    public decimal GetLineTotal()
    {
        return Product.Price * Quantity;
    }
}