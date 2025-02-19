using CurrencyConverter.Api.Application.Configuration;
using CurrencyConverter.Api.Application.Interfaces;
using CurrencyConverter.Api.Application.DTOs;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace CurrencyConverter.Api.Services
{
    public class FrankfurterExchangeRateService : IExchangeRateService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FrankfurterExchangeRateService> _logger;
        private readonly IDistributedCache _cache;

        private static readonly string[] ExcludedCurrencies = { "TRY", "PLN", "THB", "MXN" };

        public FrankfurterExchangeRateService(
            IHttpClientFactory httpClientFactory,
            IDistributedCache cache,
            ILogger<FrankfurterExchangeRateService> logger,
            IOptions<FrankfurterApiOptions> frankfurterApiOptions)
        {
            _httpClient = httpClientFactory.CreateClient("DefaultHttpClient");
            _httpClient.BaseAddress = new Uri(frankfurterApiOptions.Value.BaseUrl);
            _cache = cache;
            _logger = logger;
        }

        public async Task<Dictionary<string, decimal>> GetLatestRatesAsync(string baseCurrency)
        {
            string cacheKey = $"latest_rates_{baseCurrency}";

            var cachedRates = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedRates))
            {
                _logger.LogInformation("Returning cached latest exchange rates.");
                return JsonConvert.DeserializeObject<Dictionary<string, decimal>>(cachedRates)!;
            }

            try
            {
                var response = await _httpClient.GetAsync($"/latest?from={baseCurrency}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var rates = JsonConvert.DeserializeObject<FrankfurterApiResponse>(content)?.Rates;

                if (rates == null)
                {
                    throw new Exception("Failed to retrieve exchange rates.");
                }

                // Cache result for 10 minutes
                await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(rates), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });

                return rates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while getting latest exchange rates for {baseCurrency}");
                throw;
            }
        }

       public async Task<decimal> ConvertCurrencyAsync(string fromCurrency, string toCurrency, decimal amount)
        {
            if (ExcludedCurrencies.Contains(fromCurrency.ToUpper()) || ExcludedCurrencies.Contains(toCurrency.ToUpper()))
            {
                var errorMessage = $"Conversion involving excluded currencies (TRY, PLN, THB, MXN) is not allowed. From: {fromCurrency}, To: {toCurrency}";
                _logger.LogError(errorMessage);
                throw new ArgumentException(errorMessage);
            }

            try
            {
                var rates = await GetLatestRatesAsync(fromCurrency);
                if (!rates.ContainsKey(toCurrency))
                {
                    var errorMessage = $"Exchange rate for {toCurrency} not found.";
                    _logger.LogError(errorMessage);
                    throw new KeyNotFoundException(errorMessage);
                }

                return amount * rates[toCurrency];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while converting {amount} {fromCurrency} to {toCurrency}");
                throw;
            }
        }

        public async Task<Dictionary<string, Dictionary<string, decimal>>> GetHistoricalRatesAsync(
            string baseCurrency, DateTime startDate, DateTime endDate)
        {
            string cacheKey = $"historical_rates_{baseCurrency}_{startDate}_{endDate}";

            var cachedRates = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedRates))
            {
                _logger.LogInformation("Returning cached historical exchange rates.");
                return JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, decimal>>>(cachedRates)!;
            }

            try
            {
                var response = await _httpClient.GetAsync($"/{startDate:yyyy-MM-dd}..{endDate:yyyy-MM-dd}?from={baseCurrency}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<FrankfurterHistoricalApiResponse>(content)?.Rates;

                if (result == null)
                {
                    throw new Exception("Failed to retrieve historical exchange rates.");
                }

                // Cache result for 30 minutes in Redis
                await _cache.SetStringAsync(cacheKey, JsonConvert.SerializeObject(result), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                });


                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while getting historical exchange rates for {baseCurrency} between {startDate} and {endDate}");
                throw;
            }
        }
    }
}
