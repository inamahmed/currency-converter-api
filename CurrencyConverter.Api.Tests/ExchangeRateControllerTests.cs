using Moq;
using NUnit.Framework;
using CurrencyConverter.Api.Application.Interfaces;
using CurrencyConverter.Api.Controllers.v1;
using Microsoft.Extensions.Logging;
using CurrencyConverter.Api.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CurrencyConverter.Api.Tests
{
    [TestFixture]
    public class ExchangeRateControllerTests
    {
        private Mock<IExchangeRateServiceFactory> _mockExchangeRateServiceFactory;
        private Mock<IExchangeRateService> _mockExchangeRateService;
        private Mock<ILogger<ExchangeRateController>> _mockLogger;
        private ExchangeRateController _controller;

        [SetUp]
        public void Setup()
        {
            _mockExchangeRateServiceFactory = new Mock<IExchangeRateServiceFactory>();
            _mockExchangeRateService = new Mock<IExchangeRateService>();
            _mockLogger = new Mock<ILogger<ExchangeRateController>>();

            _controller = new ExchangeRateController(
                _mockExchangeRateServiceFactory.Object,
                _mockLogger.Object);
        }

        [Test]
        public async Task GetLatestRates_ReturnsBadRequest_WhenBaseCurrencyIsEmpty()
        {
            // Act
            var result = await _controller.GetLatestRates("1011", "");

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
            var badRequestResult = result as BadRequestObjectResult;
            Assert.AreEqual("Base currency is required.", badRequestResult.Value);
        }

        [Test]
        public async Task GetLatestRates_ReturnsOk_WhenValidRequest()
        {
            // Arrange
            _mockExchangeRateServiceFactory.Setup(f => f.CreateExchangeRateService(It.IsAny<string>())).Returns(_mockExchangeRateService.Object);

            // Mock the method that will return a sample rate
            var rates = new Dictionary<string, decimal> { { "USD", 1.0m }, { "EUR", 0.9m } };
            _mockExchangeRateService.Setup(s => s.GetLatestRatesAsync(It.IsAny<string>())).ReturnsAsync(rates);

            // Act
            var result = await _controller.GetLatestRates("1011", "USD");

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.AreEqual(rates, okResult.Value);
        }

        // Test case for the ConvertCurrency action
        [Test]
        public async Task ConvertCurrency_ReturnsBadRequest_WhenAmountIsZeroOrNegative()
        {
            // Arrange
            var request = new CurrencyConversionRequest
            {
                Amount = 0,
                FromCurrency = "USD",
                ToCurrency = "EUR",
                ServiceId = "1011"
            };

            // Act
            var result = await _controller.ConvertCurrency(request);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
            var badRequestResult = result as BadRequestObjectResult;
            Assert.AreEqual("Amount must be greater than zero.", badRequestResult.Value);
        }

        [Test]
        public async Task ConvertCurrency_ReturnsOk_WhenValidRequest()
        {
            // Arrange
            _mockExchangeRateServiceFactory.Setup(f => f.CreateExchangeRateService(It.IsAny<string>())).Returns(_mockExchangeRateService.Object);

            var request = new CurrencyConversionRequest
            {
                ServiceId = "1011",
                FromCurrency = "USD",
                ToCurrency = "EUR",
                Amount = 100
            };

            // Mock the method that will return the converted amount
            _mockExchangeRateService.Setup(s => s.ConvertCurrencyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
                .ReturnsAsync(90.0m); // Mock the return value for conversion

            // Act
            var result = await _controller.ConvertCurrency(request);

            // Assert
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);  // Ensure that the result is OkObjectResult

            // Use dynamic to access the value
            dynamic value = okResult.Value;
            Assert.AreEqual(90.0m, value.ConvertedAmount);  // Expecting the ConvertedAmount in the response
        }


        // Test case for the GetHistoricalRates action
        [Test]
        public async Task GetHistoricalRates_ReturnsBadRequest_WhenBaseCurrencyIsEmpty()
        {
            // Act
            var result = await _controller.GetHistoricalRates("1011", "", DateTime.Now, DateTime.Now);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
            var badRequestResult = result as BadRequestObjectResult;
            Assert.AreEqual("Base currency is required.", badRequestResult.Value);
        }

        [Test]
        public async Task GetHistoricalRates_ReturnsBadRequest_WhenStartDateIsLaterThanEndDate()
        {
            // Act
            var result = await _controller.GetHistoricalRates("1011", "USD", DateTime.Now, DateTime.Now.AddDays(-1));

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
            var badRequestResult = result as BadRequestObjectResult;
            Assert.AreEqual("Start date cannot be later than end date.", badRequestResult.Value);
        }

        [Test]
        public async Task GetHistoricalRates_ReturnsOk_WhenValidRequest()
        {
            // Arrange
            _mockExchangeRateServiceFactory.Setup(f => f.CreateExchangeRateService(It.IsAny<string>())).Returns(_mockExchangeRateService.Object);

            // Mock the method that will return historical rates
            var historicalRates = new Dictionary<string, Dictionary<string, decimal>>
                {
                    { "2025-02-18", new Dictionary<string, decimal> { { "USD", 1.1m }, { "EUR", 0.9m } } },
                    { "2025-02-17", new Dictionary<string, decimal> { { "USD", 1.2m }, { "EUR", 1.0m } } }
                };

            _mockExchangeRateService.Setup(s => s.GetHistoricalRatesAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(historicalRates);

            // Act
            var result = await _controller.GetHistoricalRates("1011", "USD", DateTime.Now.AddDays(-2), DateTime.Now);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.AreEqual(historicalRates, okResult.Value);
        }
    }
}
