# Замена API backend на фронте

Эта инструкция нужна, когда backend уже задеплоен и фронт должен обращаться не к локальному API, а к продовому домену.

## Текущий backend

Production API:

```text
https://domen-back-end-grafik.tw1.su
```

Swagger:

```text
https://domen-back-end-grafik.tw1.su/swagger/index.html
```

Health check:

```text
https://domen-back-end-grafik.tw1.su/health
```

## Где менять на фронте

Файл:

```text
index.html
```

Фронт должен обращаться к production backend:

```text
https://domen-back-end-grafik.tw1.su
```

## Текущее состояние

В `index.html` заведена одна константа:

```js
const API_BASE_URL = 'https://domen-back-end-grafik.tw1.su';
```

Все API-запросы идут через helper:

```js
apiFetch(path, options)
```

Задействованные endpoints:

```text
POST /api/shiftschedule/calculate
POST /api/auth/register
POST /api/auth/login
GET  /api/shiftschedule/history
GET  /api/support/tickets
POST /api/support/tickets
POST /api/support/tickets/{id}/reply
```

## Как поменять API

В `index.html` заменить значение:

```js
const API_BASE_URL = 'https://domen-back-end-grafik.tw1.su';
```

Для локальной разработки можно временно вернуть:

```js
const API_BASE_URL = 'http://localhost:5000';
```

Не добавляй `http://` fallback для production-домена, если фронт открыт по HTTPS: браузер заблокирует такие запросы как mixed content.

## CORS на backend

В Timeweb в переменных окружения backend должен быть разрешён домен фронта:

```text
Cors__AllowedOrigins__0=https://grafik-smen-cool.ru
```

Если фронт временно открывается локально, добавь локальный origin отдельной переменной:

```text
Cors__AllowedOrigins__0=https://grafik-smen-cool.ru
Cors__AllowedOrigins__1=http://localhost:3000
```

Важно: origin включает схему и домен, но не включает путь `/api`.

Проверка preflight:

```bash
curl -i -X OPTIONS https://domen-back-end-grafik.tw1.su/api/auth/login \
  -H 'Origin: https://grafik-smen-cool.ru' \
  -H 'Access-Control-Request-Method: POST' \
  -H 'Access-Control-Request-Headers: content-type'
```

В ответе должен быть заголовок:

```text
access-control-allow-origin: https://grafik-smen-cool.ru
```

## Проверка после замены

1. Открыть frontend.
2. Зарегистрировать нового пользователя.
3. Войти.
4. Рассчитать график.
5. Проверить, что история графиков появилась.
6. Оставить отзыв с оценкой.
7. Войти админом и ответить на отзыв.

Админ:

```text
admin@admin.ru
```

Пароль задаётся переменной backend:

```text
Admin__Password
```
