namespace CurrencyConverter.Api.Application.DTOs
{
    public class FrankfurterHistoricalApiResponse
    {
        public Dictionary<string, Dictionary<string, decimal>>? Rates { get; set; }
    }
}
