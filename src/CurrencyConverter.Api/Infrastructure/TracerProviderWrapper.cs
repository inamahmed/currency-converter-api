using CurrencyConverter.Api.Application.Interfaces;
using OpenTelemetry.Trace;

namespace CurrencyConverter.Api.Infrastructure
{
    public class TracerProviderWrapper : ITracerProviderWrapper
    {
        private readonly TracerProvider _tracerProvider;

        public TracerProviderWrapper(TracerProvider tracerProvider)
        {
            _tracerProvider = tracerProvider;
        }

        public Tracer GetTracer(string instrumentName, string version = null)
        {
            return _tracerProvider.GetTracer(instrumentName, version);
        }
    }
}
