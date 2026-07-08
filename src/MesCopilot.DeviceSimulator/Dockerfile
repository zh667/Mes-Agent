FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY MesCopilot.sln ./
COPY src/MesCopilot.Domain/MesCopilot.Domain.csproj src/MesCopilot.Domain/
COPY src/MesCopilot.Application/MesCopilot.Application.csproj src/MesCopilot.Application/
COPY src/MesCopilot.Infrastructure/MesCopilot.Infrastructure.csproj src/MesCopilot.Infrastructure/
COPY src/MesCopilot.DeviceSimulator/MesCopilot.DeviceSimulator.csproj src/MesCopilot.DeviceSimulator/

RUN dotnet restore src/MesCopilot.DeviceSimulator/MesCopilot.DeviceSimulator.csproj

COPY src/ src/
RUN dotnet publish src/MesCopilot.DeviceSimulator/MesCopilot.DeviceSimulator.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MesCopilot.DeviceSimulator.dll"]
