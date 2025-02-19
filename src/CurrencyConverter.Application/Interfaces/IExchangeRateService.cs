namespace CurrencyConverter.Application.Interfaces;

public interface IExchangeRateService
{
    Task<Dictionary<string, decimal>> GetLatestRatesAsync(string baseCurrency);
    Task<decimal> ConvertCurrencyAsync(string fromCurrency, string toCurrency, decimal amount);
    Task<Dictionary<string, Dictionary<string, decimal>>> GetHistoricalRatesAsync(
        string baseCurrency, DateTime startDate, DateTime endDate);
}
