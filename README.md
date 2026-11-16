# CourierPulse

Courier dispatch and delivery-tracking platform built with ASP.NET Core 8, split into a Web
API microservice and a Razor Pages frontend.

> This README is a work in progress and will be expanded with architecture, business rules,
> endpoints and setup instructions as the project is built out.

## Solution layout

- `src/CourierPulse.Api` — ASP.NET Core Web API (backend microservice)
- `src/CourierPulse.Web` — ASP.NET Core Razor Pages (frontend)
- `tests/CourierPulse.Api.Tests` — xUnit test suite for the API

## Development

```bash
dotnet build
dotnet test
```
