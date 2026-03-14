# Развёртывание Forgefit на Linux-сервере в Docker

## Архитектура

```
┌─────────────────────────────────────────────────┐
│               Docker Compose                    │
│                                                 │
│  ┌──────────────┐     ┌──────────────────────┐  │
│  │   frontend   │     │        server        │  │
│  │  nginx:80    │────▶│  Node.js API :3000   │  │
│  │  Vue SPA     │     │  Express + JWT       │  │
│  └──────────────┘     └──────────────────────┘  │
│         │                                       │
└─────────┼───────────────────────────────────────┘
          │ порт 80 (или APP_PORT)
    Интернет / браузер
```

- **frontend** — nginx, раздаёт собранный Vue SPA и проксирует `/api/` на backend
- **server** — Express API, работает внутри Docker-сети (не открыт наружу напрямую)
- Все данные хранятся в памяти сервера (in-memory DB, заменяется на реальную СУБД)

---

## Требования

| Инструмент | Минимальная версия |
|---|---|
| Linux (Ubuntu 22.04 / Debian 12) | — |
| Docker Engine | ≥ 24.0 |
| Docker Compose plugin | ≥ 2.20 |
| Открытый порт на сервере | 80 (или другой) |

---

## 1. Установка Docker

```bash
# Обновить пакеты
sudo apt update && sudo apt upgrade -y

# Зависимости
sudo apt install -y ca-certificates curl gnupg lsb-release

# GPG-ключ Docker
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg \
  | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg

# Репозиторий Docker
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
  https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" \
  | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

# Docker Engine + Compose plugin
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io \
                   docker-buildx-plugin docker-compose-plugin

# Разрешить текущему пользователю запускать docker без sudo
sudo usermod -aG docker $USER
newgrp docker

# Проверка
docker --version
docker compose version
```

---

## 2. Получение кода на сервере

### Вариант A — клонирование из Git

```bash
git clone <URL_репозитория> forgefit
cd forgefit
```

### Вариант B — загрузка через scp / rsync

```bash
# С локальной машины:
rsync -avz --exclude node_modules --exclude .git \
  ./forgefit/ user@your-server-ip:/home/user/forgefit/

# На сервере:
cd /home/user/forgefit
```

---

## 3. Конфигурация окружения

Скопируйте шаблон и заполните переменные:

```bash
cp .env.example .env
nano .env
```

Содержимое `.env`:

```env
# ОБЯЗАТЕЛЬНО — изменить JWT_SECRET перед продакшен-запуском!
JWT_SECRET=ваш-длинный-случайный-секрет-минимум-32-символа

JWT_EXPIRES_IN=7d

# Порт, на котором будет доступно приложение (по умолчанию 80)
APP_PORT=80

# CORS — укажите ваш домен (если используете обратный прокси с доменом)
CORS_ORIGIN=http://your-domain.com
```

> ⚠️ **Никогда не коммитьте `.env` в репозиторий.** Файл добавлен в `.gitignore`.

---

## 4. Сборка и запуск

```bash
# Собрать образы и запустить в фоне
docker compose up -d --build
```

Приложение будет доступно по адресу:

```
http://<IP-сервера>          (порт 80 по умолчанию)
http://<IP-сервера>:8080     (если задали APP_PORT=8080)
```

### Проверка работы API

```bash
curl http://localhost/api/health
# {"status":"ok","timestamp":"..."}
```

---

## 5. Управление контейнерами

```bash
# Статус контейнеров
docker compose ps

# Логи всех сервисов (в реальном времени)
docker compose logs -f

# Логи только бэкенда
docker compose logs -f server

# Логи только фронтенда
docker compose logs -f frontend

# Остановить без удаления
docker compose stop

# Остановить и удалить контейнеры
docker compose down

# Перезапустить после изменения кода
docker compose up -d --build
```

---

## 6. Регистрация первого пользователя

После запуска откройте приложение в браузере и зарегистрируйтесь через форму.
Каждый пользователь видит только свои упражнения и тренировки.

Через API (curl):

```bash
curl -X POST http://localhost/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"securepassword","name":"Admin"}'
```

---

## 7. Настройка HTTPS с доменом (Certbot + Nginx на хосте)

Если у вас есть домен и вы хотите HTTPS:

```bash
# Установить Nginx на хост
sudo apt install -y nginx

# Создать конфиг сайта
sudo nano /etc/nginx/sites-available/forgefit
```

Содержимое конфига:

```nginx
server {
    listen 80;
    server_name your-domain.com www.your-domain.com;

    location / {
        proxy_pass         http://127.0.0.1:80;
        proxy_http_version 1.1;
        proxy_set_header   Host              $host;
        proxy_set_header   X-Real-IP         $remote_addr;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
}
```

```bash
# Включить сайт
sudo ln -s /etc/nginx/sites-available/forgefit /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx

# Получить SSL-сертификат
sudo apt install -y certbot python3-certbot-nginx
sudo certbot --nginx -d your-domain.com -d www.your-domain.com
```

> Не забудьте в `.env` обновить `CORS_ORIGIN=https://your-domain.com` и пересобрать контейнеры.

---

## 8. Автозапуск при перезагрузке

Политика `restart: unless-stopped` уже настроена в `docker-compose.yml`.
Убедитесь, что Docker стартует вместе с системой:

```bash
sudo systemctl enable docker
```

---

## 9. Обновление приложения

```bash
# Получить обновления
git pull

# Пересобрать образы и перезапустить
docker compose up -d --build
```

---

## 10. Подключение реальной базы данных

Текущая реализация использует **in-memory хранилище** (данные сбрасываются при перезапуске сервера).

Для продакшена замените на реальную СУБД:

1. Создайте файл коннектора, реализующий интерфейс `IDatabase` из `server/src/db/interface.ts`:

```typescript
// server/src/db/postgres.ts  (пример)
import { IDatabase } from './interface.js'

export class PostgresDatabase implements IDatabase {
  // реализуйте все методы интерфейса через pg/prisma/drizzle
}
```

2. Обновите `server/src/db/index.ts`:

```typescript
import { PostgresDatabase } from './postgres.js'
export const db = new PostgresDatabase()
```

3. Добавьте переменную `DATABASE_URL` в `.env` и в сервис `server` в `docker-compose.yml`.

Поддерживаемые коннекторы (примеры): PostgreSQL (pg, Prisma, Drizzle), MongoDB (mongoose), SQLite (better-sqlite3).

---

## Структура проекта

```
forgefit/
├── src/                        # Vue 3 фронтенд
│   ├── api/                    # HTTP-клиент для запросов к API
│   │   ├── client.ts           # Базовый fetch-wrapper с авторизацией
│   │   ├── auth.ts             # Эндпоинты авторизации
│   │   ├── exercises.ts        # Эндпоинты упражнений
│   │   └── workouts.ts         # Эндпоинты тренировок
│   ├── stores/                 # Pinia-сторы
│   │   ├── auth.ts             # Авторизация (токен, пользователь)
│   │   ├── exercises.ts        # Кеш упражнений (загружается с API)
│   │   └── workouts.ts         # Сессии тренировок (загружаются с API)
│   ├── views/
│   │   ├── AuthView.vue        # Экран входа / регистрации
│   │   ├── WorkoutView.vue     # Основной экран тренировки
│   │   ├── ExercisesView.vue   # Список упражнений
│   │   └── ExerciseEditView.vue # Создание / редактирование упражнения
│   ├── router/index.ts         # Vue Router (guards авторизации)
│   └── App.vue                 # Shell: топ-бар, навигация, кнопка выхода
│
├── server/                     # Express API (Node.js + TypeScript)
│   ├── src/
│   │   ├── db/
│   │   │   ├── interface.ts    # Абстрактный интерфейс БД
│   │   │   ├── inMemory.ts     # In-memory реализация (по умолчанию)
│   │   │   └── index.ts        # Синглтон БД (замените коннектор здесь)
│   │   ├── middleware/
│   │   │   ├── auth.ts         # JWT-middleware
│   │   │   └── errorHandler.ts # Глобальная обработка ошибок
│   │   ├── routes/
│   │   │   ├── auth.ts         # POST /register, POST /login, GET /me
│   │   │   ├── exercises.ts    # CRUD /exercises
│   │   │   └── workouts.ts     # /workouts: сессии, подходы, история
│   │   ├── types/index.ts      # TypeScript-типы сервера
│   │   ├── config.ts           # Конфигурация из env
│   │   ├── app.ts              # Express app
│   │   └── index.ts            # Точка входа сервера
│   ├── Dockerfile              # Многоэтапная сборка Node.js
│   └── package.json
│
├── Dockerfile                  # Фронтенд: Node build → nginx
├── nginx.conf                  # nginx: SPA + /api proxy
├── docker-compose.yml          # Оркестрация: frontend + server
├── .env.example                # Шаблон переменных окружения
└── DEPLOY.md                   # Эта инструкция
```

---

## API-эндпоинты (краткий справочник)

| Метод | URL | Описание | Авторизация |
|---|---|---|---|
| POST | `/api/auth/register` | Регистрация | — |
| POST | `/api/auth/login` | Вход | — |
| GET | `/api/auth/me` | Текущий пользователь | ✅ |
| GET | `/api/exercises` | Список упражнений | ✅ |
| POST | `/api/exercises` | Создать упражнение | ✅ |
| PUT | `/api/exercises/:id` | Обновить упражнение | ✅ |
| DELETE | `/api/exercises/:id` | Удалить упражнение | ✅ |
| GET | `/api/workouts/today` | Сегодняшняя сессия | ✅ |
| GET | `/api/workouts/history/:exerciseId` | История упражнения | ✅ |
| POST | `/api/workouts/sets` | Добавить подход | ✅ |
| DELETE | `/api/workouts/sets/:id` | Удалить подход | ✅ |
| GET | `/api/health` | Проверка сервера | — |
