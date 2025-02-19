namespace CurrencyConverter.Api.Application.DTOs
{
    public class RateLimitData
    {
        public int RequestCount { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
