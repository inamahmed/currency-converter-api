namespace CurrencyConverter.Api.Application.Interfaces
{
    public interface IExchangeRateServiceFactory
    {
        IExchangeRateService CreateExchangeRateService(string serviceId);
    }
}
