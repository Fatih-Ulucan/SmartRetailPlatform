namespace SmartRetailPlatform.Models;

public class Product
{
    public string Name { get; }
    public decimal Price { get; }
    private int _stock;

    public Product(string name, decimal price, int initialStock)
    {
        Name = name;
        Price = price;
        _stock = initialStock;
    }
    
    public int GetStock() => _stock;

    public void ReduceStock(int amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive");
        
        if (amount > _stock)
            throw new ArgumentException($"Not enough stock for {Name}. Available : {_stock}, requested: {amount} ");
        _stock -= amount;
    }
}