using CurrencyConverter.Api.Application.Interfaces;
using CurrencyConverter.Api.Services;

namespace CurrencyConverter.Api.Infrastructure.Factories
{
    public class ExchangeRateServiceFactory : IExchangeRateServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, Type> _serviceMappings;

        public ExchangeRateServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _serviceMappings = new Dictionary<string, Type>
            {
                { "1011", typeof(FrankfurterExchangeRateService) }
            };
        }

        public IExchangeRateService CreateExchangeRateService(string serviceId)
        {
            if (string.IsNullOrWhiteSpace(serviceId))
                throw new ArgumentException($"Service id is required");

            if (_serviceMappings.TryGetValue(serviceId, out var serviceType))
            {
                return (IExchangeRateService)_serviceProvider.GetRequiredService(serviceType);
            }

            throw new ArgumentException($"Service not found for ID: {serviceId}. Valid IDs are: {string.Join(", ", _serviceMappings.Keys)}, Please check README for details.");
        }
    }
}
