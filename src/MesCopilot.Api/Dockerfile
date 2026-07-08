FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY MesCopilot.sln ./
COPY src/MesCopilot.Domain/MesCopilot.Domain.csproj src/MesCopilot.Domain/
COPY src/MesCopilot.Application/MesCopilot.Application.csproj src/MesCopilot.Application/
COPY src/MesCopilot.Infrastructure/MesCopilot.Infrastructure.csproj src/MesCopilot.Infrastructure/
COPY src/MesCopilot.Agent/MesCopilot.Agent.csproj src/MesCopilot.Agent/
COPY src/MesCopilot.Api/MesCopilot.Api.csproj src/MesCopilot.Api/

RUN dotnet restore src/MesCopilot.Api/MesCopilot.Api.csproj

COPY src/ src/
RUN dotnet publish src/MesCopilot.Api/MesCopilot.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MesCopilot.Api.dll"]

