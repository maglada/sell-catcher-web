# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["SellCatcher.Blazor/SellCatcher.Blazor.csproj", "SellCatcher.Blazor/"]
RUN dotnet restore "SellCatcher.Blazor/SellCatcher.Blazor.csproj"

# Copy source code
COPY . .
WORKDIR "/src/SellCatcher.Blazor"
RUN dotnet build "SellCatcher.Blazor.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "SellCatcher.Blazor.csproj" -c Release -o /app/publish

# Runtime stage - use nginx to serve Blazor WASM
FROM nginx:alpine
WORKDIR /app

# Copy Blazor published files
COPY --from=publish /app/publish/wwwroot /usr/share/nginx/html

# Copy nginx config
COPY SellCatcher.Docker/nginx.conf /etc/nginx/nginx.conf

EXPOSE 3000
CMD ["nginx", "-g", "daemon off;"]
