# Production — Elektrika

## Что получится после деплоя

| URL | Сервис |
|-----|--------|
| `https://ваш-домен.ru` | Сайт + API (заявки, прайс, калькулятор) |
| `https://admin.ваш-домен.ru` | Админ-панель |
| PostgreSQL, RabbitMQ | Внутри Docker, снаружи не торчат |

Всё работает **24/7**, перезапускается после падения (`restart: unless-stopped`), HTTPS через **Caddy** (Let's Encrypt автоматически).

---

## Требования к серверу (VPS)

- Ubuntu 22.04 / 24.04 (или другой Linux с Docker)
- **2 GB RAM** минимум (лучше 4 GB)
- **20 GB** диск
- Открыты порты **80** и **443**
- Домен с DNS A-записью на IP сервера:
  - `elektromontazh.ru` → IP
  - `admin.elektromontazh.ru` → IP

---

## Быстрый деплой (5 шагов)

### 1. Установить Docker на сервер

```bash
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER
# перелогиниться
```

### 2. Загрузить проект на сервер

```bash
git clone https://github.com/eg0r112/Elektrika1.git
cd Elektrika1/server
```

### 3. Настроить `.env`

```bash
cp .env.production.example .env
nano .env
```

Заполнить **все** значения — особенно:
- `DOMAIN`, `ADMIN_DOMAIN`, `ACME_EMAIL`
- `POSTGRES_PASSWORD`, `RABBITMQ_PASSWORD`, `JWT_SECRET`
- `ADMIN_INITIAL_PASSWORD` (логин admin создаётся при первом запуске)
- `TELEGRAM_BOT_TOKEN`, `TELEGRAM_CHAT_IDS` (через запятую — несколько получателей)

### 4. Запустить

```bash
chmod +x deploy.sh
./deploy.sh
```

Или вручную:

```bash
docker compose -f docker-compose.prod.yml up -d --build
```

### 5. Проверить

```bash
docker compose -f docker-compose.prod.yml ps
curl -s https://ваш-домен.ru/health
```

Открыть в браузере:
- `https://ваш-домен.ru` — сайт
- `https://admin.ваш-домен.ru` — админка (логин **admin** + пароль из `ADMIN_INITIAL_PASSWORD`)

---

## Архитектура

```
Интернет
   │
   ▼
 Caddy (:443 HTTPS)
   ├── ваш-домен.ru      → api:8080   (сайт + REST API)
   └── admin.ваш-домен.ru → admin:8080 (админка)

api ──► PostgreSQL (заявки, прайс)
  │
  └──► RabbitMQ ──► Consumer ──► Telegram
```

- Заявка **сначала** в PostgreSQL, потом в очередь
- **Publisher confirms** — RabbitMQ подтверждает запись в очередь
- Если RabbitMQ был недоступен — **republish worker** повторит из БД

---

## Управление на сервере

```bash
cd /path/to/Elektrika1/server

# Логи API
docker compose -f docker-compose.prod.yml logs -f api

# Перезапуск после изменений
docker compose -f docker-compose.prod.yml up -d --build

# Остановить всё
docker compose -f docker-compose.prod.yml down

# Остановить и удалить данные БД (осторожно!)
docker compose -f docker-compose.prod.yml down -v
```

### Бэкап PostgreSQL

```bash
docker compose -f docker-compose.prod.yml exec postgres \
  pg_dump -U elektrika elektrika > backup_$(date +%F).sql
```

### Обновление после git pull

```bash
git pull
docker compose -f docker-compose.prod.yml up -d --build
```

---

## Локальная разработка (без production)

```bash
cd server
docker compose up -d          # только Postgres + RabbitMQ
cd src/Elektrika.Api
dotnet run                    # http://localhost:5086
```

Секреты — в `appsettings.Development.local.json` (не коммитится).

---

## Безопасность

- Секреты **только** в `.env` на сервере — не в git
- PostgreSQL и RabbitMQ **не** проброшены наружу (только internal network)
- JWT, CORS, rate limiting, honeypot — включены
- Смените `ADMIN_INITIAL_PASSWORD` после первого входа (через БД или пересоздание)

---

## Файлы деплоя

| Файл | Назначение |
|------|------------|
| `server/docker-compose.prod.yml` | Production-стек |
| `server/Dockerfile.api` | Образ сайта + API |
| `server/Dockerfile.admin` | Образ админки |
| `server/deploy/Caddyfile` | HTTPS reverse proxy |
| `server/.env.production.example` | Шаблон переменных |
| `server/deploy.sh` | Скрипт запуска |
