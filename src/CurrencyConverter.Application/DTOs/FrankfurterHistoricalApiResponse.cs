namespace CurrencyConverter.Application.DTOs
{
    public class FrankfurterHistoricalApiResponse
    {
        public Dictionary<string, Dictionary<string, decimal>>? Rates { get; set; }
    }
}
