FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/C-TalentLens.Api/C-TalentLens.Api.csproj", "src/C-TalentLens.Api/"]
COPY ["src/C-TalentLens.Domain/C-TalentLens.Domain.csproj", "src/C-TalentLens.Domain/"]
COPY ["src/C-TalentLens.Application/C-TalentLens.Application.csproj", "src/C-TalentLens.Application/"]
COPY ["src/C-TalentLens.Infrastructure/C-TalentLens.Infrastructure.csproj", "src/C-TalentLens.Infrastructure/"]
RUN dotnet restore "src/C-TalentLens.Api/C-TalentLens.Api.csproj"

COPY . .
RUN dotnet publish "src/C-TalentLens.Api/C-TalentLens.Api.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__TalentLens="Data Source=/app/data/talentlens.db"
ENV Hangfire__DatabasePath="/app/data/hangfire.db"

RUN mkdir -p /app/data && chown -R $APP_UID /app

COPY --from=build /app/publish .

USER $APP_UID
EXPOSE 8080

ENTRYPOINT ["dotnet", "C-TalentLens.dll"]
