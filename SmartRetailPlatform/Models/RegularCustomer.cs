namespace SmartRetailPlatform.Models;

public class RegularCustomer : Customer 
{
    private const decimal DiscountRate = 0.05m;

    public RegularCustomer(string name, string email) : base(name, email)
    {
    }

    public override decimal CalculateDiscount(decimal orderTotal)
    {
        return orderTotal * DiscountRate;
    }
}