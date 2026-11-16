FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/CourierPulse.Api/CourierPulse.Api.csproj src/CourierPulse.Api/
RUN dotnet restore src/CourierPulse.Api/CourierPulse.Api.csproj

COPY src/CourierPulse.Api/ src/CourierPulse.Api/
RUN dotnet publish src/CourierPulse.Api/CourierPulse.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CourierPulse.Api.dll"]
