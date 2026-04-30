# Shift Schedule Calculator MVP

Этот проект содержит бэкенд на ASP.NET Core 10 и фронтенд на Vue.js для расчёта графика смен.

Структура проекта:
- `backend/` — ASP.NET Core Web API с Identity, JWT, EF Core и SQLite
- `frontend/` — одностраничное приложение Vue.js в `index.html`

В проекте реализована:
- регистрация и вход пользователей
- сохранение последних 5 графиков авторизованного пользователя
- расчёт графика смен и вывод результата

Запуск:
1. Перейдите в каталог `backend`
2. Установите .NET SDK и Xcode CLI tools, если требуется
3. Запустите `dotnet run --project src/WebApi`
4. Откройте `frontend/index.html` в браузере или используйте локальный сервер
