namespace CurrencyConverter.Application.Interfaces
{
    public interface IExchangeRateServiceFactory
    {
        IExchangeRateService CreateExchangeRateService(string serviceId);
    }
}
