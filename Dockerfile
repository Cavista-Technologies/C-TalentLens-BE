# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["C-TalentLens.csproj", "."]
RUN dotnet restore "./C-TalentLens.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./C-TalentLens.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./C-TalentLens.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN mkdir -p /app/data && chown -R $APP_UID /app/data

ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__TalentLens="Data Source=/app/data/TalentLens.db"
ENTRYPOINT ["dotnet", "C-TalentLens.dll"]