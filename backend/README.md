# Shift Schedule Calculator - Backend (MVP)

ASP.NET Core Web API для расчета графика смен по ТЗ MVP.

## Структура проекта

- **Domain**: Сущности и интерфейсы репозиториев
- **Application**: Сервисы, DTO, бизнес-логика
- **Infrastructure**: Реализация репозиториев, EF Core, PostgreSQL
- **WebApi**: Контроллеры, конфигурация

## Запуск

1. Установите PostgreSQL и создайте базу `ShiftScheduleDb`
2. Обновите строку подключения в `appsettings.json`
3. Запустите миграции:
   ```
   dotnet ef database update --project src/Infrastructure --startup-project src/WebApi
   ```
4. Запустите проект:
   ```
   dotnet run --project src/WebApi
   ```
5. API доступно на https://localhost:5001/swagger

## API

- POST /api/shiftschedule/calculate
  - Тело: ShiftScheduleRequest
  - Ответ: ShiftScheduleResponse