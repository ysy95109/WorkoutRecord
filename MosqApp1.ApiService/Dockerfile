# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0-noble AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MosqApp1.ApiService/MosqApp1.ApiService.csproj", "MosqApp1.ApiService/"]
COPY ["MosqApp1.ServiceDefaults/MosqApp1.ServiceDefaults.csproj", "MosqApp1.ServiceDefaults/"]
RUN dotnet restore "./MosqApp1.ApiService/MosqApp1.ApiService.csproj"
COPY . .
WORKDIR "/src/MosqApp1.ApiService"
RUN dotnet build "./MosqApp1.ApiService.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MosqApp1.ApiService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MosqApp1.ApiService.dll"]