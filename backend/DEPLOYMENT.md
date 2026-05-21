# Backend deployment

## 1. Publish

From the repository root:

```bash
dotnet publish backend/src/WebApi/WebApi.csproj -c Release -o backend/publish
```

Copy the contents of `backend/publish` to the server, for example:

```text
/opt/shiftcalc/backend
```

## 2. Environment

Create `/etc/shiftcalc/shiftcalc-api.env` from:

```text
deploy/backend/shiftcalc-api.env.example
```

Required production values:

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:5000
ConnectionStrings__DefaultConnection=Data Source=/var/lib/shiftcalc/shiftschedule.db
Jwt__Issuer=ShiftCalcApi
Jwt__Key=<long random secret, at least 32 characters>
Admin__Email=admin@admin.ru
Admin__Password=<strong password>
Cors__AllowedOrigins__0=https://your-frontend-domain.ru
Swagger__Enabled=false
```

The app intentionally refuses to start in production without `Jwt__Key`, `Admin__Password`, and `Cors__AllowedOrigins`.

## 3. SQLite storage

For the first release SQLite is enough if the database file is stored outside the app folder:

```bash
sudo mkdir -p /var/lib/shiftcalc
sudo chown shiftcalc:shiftcalc /var/lib/shiftcalc
```

Back up this file regularly:

```text
/var/lib/shiftcalc/shiftschedule.db
```

## 4. systemd

Copy the service template:

```bash
sudo cp deploy/backend/shiftcalc-api.service.example /etc/systemd/system/shiftcalc-api.service
sudo systemctl daemon-reload
sudo systemctl enable shiftcalc-api
sudo systemctl start shiftcalc-api
sudo systemctl status shiftcalc-api
```

Logs:

```bash
sudo journalctl -u shiftcalc-api -f
```

## 5. Nginx

Copy and edit:

```bash
sudo cp deploy/backend/nginx.shiftcalc-api.conf.example /etc/nginx/sites-available/shiftcalc-api
sudo ln -s /etc/nginx/sites-available/shiftcalc-api /etc/nginx/sites-enabled/shiftcalc-api
sudo nginx -t
sudo systemctl reload nginx
```

Then enable HTTPS with Certbot:

```bash
sudo certbot --nginx -d api.example.com
```

## 6. Local development

For local runs use the development environment so `appsettings.Development.json` is loaded:

```bash
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5000 dotnet run --project backend/src/WebApi/WebApi.csproj
```

## 7. Timeweb Cloud App Platform

The repository contains a root `Dockerfile` for Timeweb Cloud App Platform.

Recommended settings:

```text
Type: Dockerfile
Project directory: /
Dockerfile path: Dockerfile
Container port: 8080
```

Required runtime environment variables:

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:8080
Jwt__Issuer=ShiftCalcApi
Jwt__Key=<long random secret, at least 32 characters>
Admin__Email=admin@admin.ru
Admin__Password=<strong password>
Cors__AllowedOrigins__0=https://your-frontend-domain.ru
Swagger__Enabled=false
```

The Docker image uses SQLite at:

```text
/app/data/shiftschedule.db
```

For production data that must survive redeploys, prefer a VPS/systemd deployment or move the database to PostgreSQL.

If Timeweb uses the automatic .NET builder instead of the root `Dockerfile`, keep the repository root as the build directory. The root `ShiftScheduleCalculator.sln` exists so automatic commands like `dotnet restore` and `dotnet build` can resolve the backend projects.

For manual automatic-builder commands use:

```bash
dotnet restore ShiftScheduleCalculator.sln
dotnet publish backend/src/WebApi/WebApi.csproj -c Release -o /app/publish
```

Start command:

```bash
dotnet /app/publish/WebApi.dll
```

Health check path:

```text
/health
```

Do not use `dotnet run` in production. It is a development command and can start too slowly for App Platform health checks.
