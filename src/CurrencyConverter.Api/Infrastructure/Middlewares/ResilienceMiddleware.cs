using CurrencyConverter.Api.Application.Configuration;
using Polly;
namespace CurrencyConverter.Api.Infrastructure.Middlewares
{
    public static class ResilienceMiddleware
    {
        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(PollyOptions settings)
        {
            return Policy
                .Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(r => (int)r.StatusCode >= 500)
                .WaitAndRetryAsync(settings.RetryCount, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }

        public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(PollyOptions settings)
        {
            return Policy
                .Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(r => (int)r.StatusCode >= 500)
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: settings.BreakAfterAttempts,
                    durationOfBreak: TimeSpan.FromSeconds(settings.BreakDurationSeconds));
        }
    }
}
