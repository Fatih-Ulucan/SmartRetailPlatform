namespace SmartRetailPlatform.Models;

public class VIPCustomer : Customer 
{
    private const decimal DiscountRate = 0.15m;
    private const decimal FreeShippingThreshold = 200m;

    public VIPCustomer(string name, string email) : base(name, email)
    {
    }

    public override decimal CalculateDiscount(decimal orderTotal)
    {
        decimal discount = orderTotal * DiscountRate;

        if (orderTotal >= FreeShippingThreshold)
        {
            discount += 20m;
        }
        return discount;
    }
}