using CurrencyConverter.Api.Application.DTOs;
using Microsoft.Extensions.Caching.Memory;

namespace CurrencyConverter.Api.Infrastructure.Middlewares
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly int _requestLimit;
        private readonly TimeSpan _timeWindow;

        public RateLimitingMiddleware(RequestDelegate next,
                                      ILogger<RateLimitingMiddleware> logger,
                                      IMemoryCache memoryCache,
                                      IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _memoryCache = memoryCache;
            _requestLimit = configuration.GetValue<int>("RateLimiting:RequestLimit", 10);
            _timeWindow = TimeSpan.FromSeconds(configuration.GetValue<int>("RateLimiting:TimeWindowSeconds", 60));
          
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString();

            if (clientIp == null)
            {
                await _next(context); 
                return;
            }

            var cacheKey = $"RLT:{clientIp}";

            var requestData = _memoryCache.Get<RateLimitData>(cacheKey);

            if (requestData == null)
            {
                requestData = new RateLimitData
                {
                    RequestCount = 1,
                    Timestamp = DateTime.UtcNow
                };
                _memoryCache.Set(cacheKey, requestData, _timeWindow);
            }
            else
            {
                if (DateTime.UtcNow - requestData.Timestamp <= _timeWindow)
                {
                    if (requestData.RequestCount >= _requestLimit)
                    {
                        _logger.LogWarning("Rate limit exceeded for IP: {ClientIp}", clientIp);
                        context.Response.StatusCode = 429; // Too Many Requests
                        await context.Response.WriteAsync("Rate limit exceeded. Try again later.");
                        return;
                    }

                    requestData.RequestCount++; // for with in window
                }
                else
                {
                    // reset if window expires
                    requestData.RequestCount = 1;
                    requestData.Timestamp = DateTime.UtcNow;
                }

                _memoryCache.Set(cacheKey, requestData, _timeWindow);
            }

            await _next(context);
        }
    }
}