namespace CurrencyConverter.Api.Application.Configuration
{
    public class PollyOptions
    {
        public int RetryCount { get; set; }
        public int BreakAfterAttempts { get; set; }
        public int BreakDurationSeconds { get; set; }
    }
}
