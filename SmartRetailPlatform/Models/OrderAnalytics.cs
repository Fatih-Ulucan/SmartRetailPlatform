namespace SmartRetailPlatform.Models;

public class OrderAnalytics
{
    public Dictionary<string, Dictionary<string, int>> BuildCoOccurrenceMap(List<List<string>> pastOrders)
    {
        var coOccurrence = new Dictionary<string, Dictionary<string, int>>();

        foreach (var order in pastOrders)
        {
            for (int i = 0; i < order.Count; i++)
            {
                for (int j = 0; j < order.Count; j++)
                {
                    if (i == j) continue;

                    string productA = order[i];
                    string productB = order[j];

                    if (!coOccurrence.ContainsKey(productA))
                    {
                        coOccurrence[productA] = new Dictionary<string, int>();
                    }

                    if (!coOccurrence[productA].ContainsKey(productB))
                    {
                        coOccurrence[productA][productB] = 0;
                    }

                    coOccurrence[productA][productB]++;
                }
            }
        }

        return coOccurrence;
    }

    public List<string> GetTopRecommendations(
        Dictionary<string, Dictionary<string, int>> coOccurrence,
        string productName,
        int topN = 3)
    {
        if (!coOccurrence.ContainsKey(productName))
        {
            return new List<string>();
        }

        return coOccurrence[productName]
            .OrderByDescending(pair => pair.Value)
            .Take(topN)
            .Select(pair => pair.Key)
            .ToList();
    }
}