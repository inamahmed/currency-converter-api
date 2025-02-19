using OpenTelemetry.Trace;

namespace CurrencyConverter.Api.Application.Interfaces
{
    public interface ITracerProviderWrapper
    {
        Tracer GetTracer(string instrumentName, string version = null);
    }
}
