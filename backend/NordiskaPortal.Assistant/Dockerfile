FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY NordiskaPortal.Assistant.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

USER app

EXPOSE 8080
ENTRYPOINT ["dotnet", "NordiskaPortal.Assistant.dll"]