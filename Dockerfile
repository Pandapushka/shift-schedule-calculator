FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY backend/ShiftScheduleCalculator.sln backend/
COPY backend/src/Domain/Domain.csproj backend/src/Domain/
COPY backend/src/Application/Application.csproj backend/src/Application/
COPY backend/src/Infrastructure/Infrastructure.csproj backend/src/Infrastructure/
COPY backend/src/WebApi/WebApi.csproj backend/src/WebApi/

RUN dotnet restore backend/src/WebApi/WebApi.csproj

COPY backend/ backend/

WORKDIR /src/backend
RUN dotnet publish src/WebApi/WebApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app/publish
RUN mkdir -p /app/data /app/publish

COPY --from=build /app/publish /app/publish

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/shiftschedule.db"

EXPOSE 8080

ENTRYPOINT ["dotnet", "/app/publish/WebApi.dll"]
