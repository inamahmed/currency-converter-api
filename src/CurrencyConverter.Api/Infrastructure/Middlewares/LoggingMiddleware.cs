using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;

namespace CurrencyConverter.Api.Infrastructure.Middlewares
{
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LoggingMiddleware> _logger;

        public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var correlationId = Guid.NewGuid().ToString();

            context.Items["CorrelationId"] = correlationId;
            var clientIp = context.Connection.RemoteIpAddress?.ToString();
            var httpMethod = context.Request.Method;
            var endpoint = context.Request.Path;

            string clientId = null;
            if (context.User.Identity.IsAuthenticated)
            {
                var jwtToken = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (!string.IsNullOrEmpty(jwtToken))
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;
                    clientId = jsonToken?.Claims.FirstOrDefault(c => c.Type == "client_id")?.Value;
                }
            }

            _logger.LogInformation($"Request received - CorrelationId: {correlationId}, IP: {clientIp}, ClientId: {clientId}, Method: {httpMethod}, Endpoint: {endpoint}");

            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-Correlation-Id"] = correlationId;
                return Task.CompletedTask;
            });

            await _next(context);

            stopwatch.Stop();
            var responseCode = context.Response.StatusCode;
            var responseTime = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation($"Response sent - CorrelationId: {correlationId}, IP: {clientIp}, ClientId: {clientId}, Method: {httpMethod}, Endpoint: {endpoint}, StatusCode: {responseCode}, ResponseTime: {responseTime} ms");
        }
    }
}