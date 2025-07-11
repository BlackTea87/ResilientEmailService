# Resilient Email Service

A resilient email sending service with retry logic, provider fallback, idempotency, rate limiting, and circuit breaker pattern.

## Features

- Two mock email providers with configurable failure rates
- Exponential backoff retry mechanism
- Automatic fallback between providers
- Idempotency support to prevent duplicate sends
- Rate limiting to prevent overloading providers
- Status tracking for email sending attempts
- Circuit breaker pattern to avoid cascading failures
- Background queue for asynchronous processing

## Setup

1. Ensure you have .NET 6 SDK installed
2. Clone this repository
3. Navigate to the project directory
4. Run the application:

```bash
dotnet run
