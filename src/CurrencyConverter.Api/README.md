Currency Converter API
A .NET 8 based Currency Converter API with Exchange Rate Retrieval, Currency Conversion, Historical Data, and Resilience Features like Rate Limiting, Circuit Breaker, and Retry Policies.

Setup Instructions
	1️. Prerequisites
	.NET 8 SDK installed
	Docker Desktop (for Jaeger & Seq)
	Redis (for rate limiting)

Configure Environment
	Modify appsettings.json for:
	Rate Limiting Settings
	Redis Configuration
	Logging Configuration
	Tracing Configuration

Assumptions Made
Authentication:
Admin User: Username ="inamadmin" and Password = "admin"
Regular User:Username ="user" and Password = "user"

Role-Based Access Control (RBAC):

Admin: Can access ConvertCurrency
User: Can only access Latest & Historical Rates

Tracing Using Jaeger:
Traces API requests using Jaeger UI at http://localhost:16686/


Rate Limiting:
Applied IP-wise
Configurable via appsettings.json

Logging with Seq:
Logs are available at http://localhost:5341/

Resilience with Circuit Breaker & Retry Policies:
For HTTP Status Code 500 & Above:
Circuit Breaker will open after multiple failures
Retry Policy will retry failed requests


Exchange Rate Service Selection:
Uses Service ID to determine which exchange rate provider to use.
Currently Supported: FrankfurterExchangeRateService (Service ID: 1011)