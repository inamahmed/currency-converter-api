using OpenTelemetry.Trace;
using System.Diagnostics;

namespace CurrencyConverter.Api.Infrastructure.Middlewares
{
    public class TracingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Tracer _tracer;

        public TracingMiddleware(RequestDelegate next, TracerProvider tracerProvider)
        {
            _next = next;
            _tracer = tracerProvider.GetTracer("CurrencyConverterApi");
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var traceId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString();

            using (var span = _tracer.StartActiveSpan(context.Request.Path))
            {
                try
                {
                    context.Response.Headers.Add("X-Trace-Id", traceId);
                    await _next(context);
                }
                catch (Exception ex)
                {
                    span.SetStatus(OpenTelemetry.Trace.Status.Error);
                    span.RecordException(ex);
                    throw;
                }
            }
        }
    }
}
