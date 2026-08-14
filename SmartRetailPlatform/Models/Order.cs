using System.Collections.Generic;
using System.Linq;

namespace SmartRetailPlatform.Models;

public class Order
{
    public Customer Customer { get; }
    private readonly List<OrderItem> _items = new();

    public Order(Customer customer)
    {
        Customer = customer;
    }

    public void AddItem(Product product, int quantity)
    {
        product.ReduceStock(quantity);
        _items.Add(new OrderItem(product, quantity));
    }

    public decimal GetSubtotal()
    {
        return _items.Sum(item => item.GetLineTotal());
    }

    public decimal GetTotalAfterDiscount()
    {
        decimal subtotal = GetSubtotal();
        decimal discount = Customer.CalculateDiscount(subtotal);
        return subtotal - discount;
    }

    public IReadOnlyList<OrderItem> GetItems()
    {
        return _items.AsReadOnly();
    }

}