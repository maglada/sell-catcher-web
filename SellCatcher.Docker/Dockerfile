#BASE SETUP
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

#Containerize app as BUILD
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SellCatcher.Api/SellCatcher.Api.csproj", "SellCatcher.Api/"]
RUN dotnet restore "SellCatcher.Api/SellCatcher.Api.csproj"
COPY . .
WORKDIR "/src/SellCatcher.Api"
RUN dotnet build "SellCatcher.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

#as Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SellCatcher.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SellCatcher.Api.dll"]