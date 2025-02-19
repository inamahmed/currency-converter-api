namespace CurrencyConverter.Api.Application.DTOs
{
    public class CurrencyConversionRequest
    {
        public string FromCurrency { get; set; } = string.Empty;
        public string ToCurrency { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string ServiceId { get; set; }
    }
}
