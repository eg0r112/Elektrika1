# Elektrika Server (.NET 9 + PostgreSQL)

## Архитектура

```
Elektrika.Domain          — сущности (прайс, заявки, админ)
Elektrika.Application     — интерфейсы, DTO
Elektrika.Infrastructure  — EF Core + PostgreSQL, сервисы, Telegram
Elektrika.Api             — REST API для сайта
Elektrika.Admin           — админ-панель (Razor Pages)
```

**База данных: PostgreSQL** — здесь хранятся прайс и заявки.

## Быстрый старт

### 1. PostgreSQL

**Вариант A — локальный PostgreSQL** (у вас уже установлен PostgreSQL 18):

```sql
CREATE USER elektrika WITH PASSWORD 'ваш_пароль';
CREATE DATABASE elektrika OWNER elektrika;
GRANT ALL PRIVILEGES ON DATABASE elektrika TO elektrika;
```

Служба должна быть запущена: `postgresql-x64-18` → Running.

**Вариант B — Docker** (нужен запущенный Docker Desktop):

```bash
cd server
docker compose up -d
```

### 2. Строка подключения

`appsettings.json` (Api и Admin):

```
Host=localhost;Port=5432;Database=elektrika;Username=elektrika;Password=elektrika
```

Для production — задайте через переменную окружения:

```bash
set ConnectionStrings__DefaultConnection=Host=...;Database=...;Username=...;Password=...
```

### 3. Миграции

При первом запуске API миграции применяются автоматически (`DbInitializer`).

Вручную:

```bash
cd server
dotnet ef database update --project src/Elektrika.Infrastructure --startup-project src/Elektrika.Api
```

### 4. Запуск

**API** — `http://localhost:5086`

```bash
cd server/src/Elektrika.Api
dotnet run
```

**Админка** — `http://localhost:5090`

```bash
cd server/src/Elektrika.Admin
dotnet run
```

Логин: **admin** / **admin123**

## Таблицы в PostgreSQL

| Таблица | Содержимое |
|---------|------------|
| `PriceCategories` | Категории прайса |
| `PriceItems` | Позиции прайса (цены) |
| `Surcharges` | Надбавки (%, высота и т.д.) |
| `OrderRequests` | Заявки с сайта |
| `AdminUsers` | Администраторы |

## Telegram

```json
"Telegram": {
  "Enabled": true,
  "BotToken": "токен",
  "ChatId": "id чата"
}
```

## API

| Метод | URL | Описание |
|-------|-----|----------|
| GET | `/health` | Проверка API + БД |
| GET | `/api/prices` | Прайс |
| POST | `/api/orders` | Новая заявка |
| POST | `/api/auth/login` | JWT |
| GET | `/api/admin/orders` | Заявки (JWT) |
| PUT | `/api/admin/prices/{id}` | Редактирование цены (JWT) |

## Production

Полная инструкция деплоя на VPS: **[PRODUCTION.md](../PRODUCTION.md)**

Кратко на сервере:

```bash
cd server
cp .env.production.example .env   # заполнить секреты и домен
./deploy.sh
```
