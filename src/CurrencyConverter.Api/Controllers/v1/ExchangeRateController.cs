using CurrencyConverter.Api.Application.DTOs;
using CurrencyConverter.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverter.Api.Controllers.v1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class ExchangeRateController : ControllerBase
    {
        private readonly IExchangeRateServiceFactory _exchangeRateServiceFactory;
        private readonly ILogger<ExchangeRateController> _logger;

        public ExchangeRateController(IExchangeRateServiceFactory exchangeRateServiceFactory,
                                       ILogger<ExchangeRateController> logger)
        {
            _exchangeRateServiceFactory = exchangeRateServiceFactory;
            _logger = logger;
        }

        [HttpGet("latest")]
        [Authorize(Policy = "UserPolicy")]
        public async Task<IActionResult> GetLatestRates([FromQuery] string serviceId, [FromQuery] string baseCurrency)
        {
            if (string.IsNullOrEmpty(baseCurrency))
            {
                return BadRequest("Base currency is required.");
            }

            try
            {
                var service = _exchangeRateServiceFactory.CreateExchangeRateService(serviceId);
                var rates = await service.GetLatestRatesAsync(baseCurrency);
                return Ok(rates);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [Authorize(Policy = "AdminPolicy")]
        [HttpPost("convert")]
        public async Task<IActionResult> ConvertCurrency([FromBody] CurrencyConversionRequest request)
        {
            if (request.Amount <= 0)
            {
                return BadRequest("Amount must be greater than zero.");
            }

            try
            {
                var service = _exchangeRateServiceFactory.CreateExchangeRateService(request.ServiceId);
                var convertedAmount = await service.ConvertCurrencyAsync(
                request.FromCurrency, request.ToCurrency, request.Amount);

                return Ok(new { ConvertedAmount = convertedAmount });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Exchange rate not found for the target currency.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("historical")]
        [Authorize(Policy = "UserPolicy")]
        public async Task<IActionResult> GetHistoricalRates([FromQuery] string serviceId,
            [FromQuery] string baseCurrency, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (string.IsNullOrEmpty(baseCurrency))
            {
                return BadRequest("Base currency is required.");
            }

            if (startDate > endDate)
            {
                return BadRequest("Start date cannot be later than end date.");
            }

            try
            {
                var service = _exchangeRateServiceFactory.CreateExchangeRateService(serviceId);
                var historicalRates = await service.GetHistoricalRatesAsync(baseCurrency, startDate, endDate);
                var pagedRates = historicalRates
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToDictionary(x => x.Key, x => x.Value);

                return Ok(pagedRates);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
