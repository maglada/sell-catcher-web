# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["SellCatcher.Api/SellCatcher.Api.csproj", "SellCatcher.Api/"]
RUN dotnet restore "SellCatcher.Api/SellCatcher.Api.csproj"

# Copy source code
COPY . .
WORKDIR "/src/SellCatcher.Api"
RUN dotnet build "SellCatcher.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "SellCatcher.Api.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "SellCatcher.Api.dll"]
