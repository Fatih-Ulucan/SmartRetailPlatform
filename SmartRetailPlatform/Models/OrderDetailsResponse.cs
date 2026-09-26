namespace SmartRetailPlatform.Models;

public class OrderDetailsResponse
{
    public int OrderId { get; set; }
    public string  CustomerName { get; set; } = string.Empty;
    public string CustomerType { get; set; } = string.Empty;
    public List<OrderItemResponse> Items { get; set; } = new();
    public decimal SubTotal => Items.Sum(i => i.LineTotal);
    
}

public class OrderItemResponse
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;
}