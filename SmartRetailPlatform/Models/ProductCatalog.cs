namespace SmartRetailPlatform.Models;

public class ProductCatalog
{
    private readonly List<Product> _productsByPrice;

    public ProductCatalog(List<Product> products)
    {
        _productsByPrice = products.OrderBy(p => p.Price).ToList();
    }

    public Product? FindByExactPrice(decimal targetPrice)
    {
        int left = 0;
        int right = _productsByPrice.Count - 1;

        while (left <= right)
        {
            int mid = left + (right - left) / 2;
            decimal midPrice = _productsByPrice[mid].Price;

            if (midPrice == targetPrice)
            {
                return _productsByPrice[mid];
            }
            else if (midPrice < targetPrice)
            {
                left = mid + 1;
            }
            else
            {
                right = mid - 1;
            }
        }

        return null;
    }
}