# Развёртывание Forgefit на Linux-сервере в Docker

## Архитектура

```
┌──────────────────────────────────────────────────┐
│                  Docker Compose                  │
│                                                  │
│  ┌───────────────┐     ┌──────────────────────┐  │
│  │   frontend    │     │        server        │  │
│  │  nginx :80    │────▶│  ASP.NET Core 8 API  │  │
│  │  Vue 3 SPA    │     │  C# + JWT  :3000     │  │
│  └───────────────┘     └──────────┬───────────┘  │
│         │                         │              │
│         │                  volume: forgefit_data │
│         │                  /app/data/*.xlsx       │
└─────────┼────────────────────────────────────────┘
          │ порт 80 (или APP_PORT)
    Интернет / браузер
```

- **frontend** — nginx раздаёт Vue 3 SPA и проксирует `/api/` на бэкенд
- **server** — ASP.NET Core 8 Web API (C#), работает внутри Docker-сети
- **xlsx-база** — каждый пользователь получает свой `.xlsx` файл, данные хранятся в Docker volume

---

## Требования к серверу

| Компонент | Минимум |
|---|---|
| Linux (Ubuntu 22.04 / Debian 12) | — |
| Docker Engine | ≥ 24.0 |
| Docker Compose plugin | ≥ 2.20 |
| RAM | 512 MB |
| Открытый порт | 80 (или другой) |

---

## 1. Установка Docker

```bash
# Обновить пакеты
sudo apt update && sudo apt upgrade -y

# Зависимости
sudo apt install -y ca-certificates curl gnupg

# GPG-ключ Docker
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg \
  | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg

# Репозиторий Docker
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
  https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo $VERSION_CODENAME) stable" \
  | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

# Docker Engine + Compose plugin
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io \
                   docker-buildx-plugin docker-compose-plugin

# Разрешить запускать Docker без sudo
sudo usermod -aG docker $USER
newgrp docker

# Проверка
docker --version        # Docker version 26.x.x
docker compose version  # Docker Compose version v2.x.x
```

---

## 2. Получение кода

### Вариант A — клонирование из Git

```bash
git clone https://github.com/artembrizinskij/Forgefit.git forgefit
cd forgefit
```

### Вариант B — загрузка через rsync с локальной машины

```bash
# С локальной машины (Git Bash / WSL):
rsync -avz \
  --exclude node_modules \
  --exclude .git \
  --exclude 'server/Forgefit.Api/bin' \
  --exclude 'server/Forgefit.Api/obj' \
  --exclude 'server/Forgefit.Api/data' \
  ./forgefit/ user@YOUR_SERVER_IP:/home/user/forgefit/

cd /home/user/forgefit
```

---

## 3. Конфигурация

```bash
cp .env.example .env
nano .env
```

Минимальный `.env` для продакшена:

```env
# Обязательно — минимум 32 символа!
JWT_SECRET=$(openssl rand -base64 32)
JWT_EXPIRES_IN=7d

# Порт (по умолчанию 80)
APP_PORT=80

# Ваш домен или IP
CORS_ORIGIN=http://YOUR_SERVER_IP
```

> ⚠️ **Никогда не коммитьте `.env` в репозиторий.**

---

## 4. Сборка и запуск

```bash
docker compose up -d --build
```

Первая сборка занимает 3–5 минут (скачивает .NET SDK и Node.js образы).

```bash
# Статус контейнеров
docker compose ps

# Проверка API
curl http://localhost/api/health
# {"status":"ok","timestamp":"..."}

# Логи в реальном времени
docker compose logs -f
```

Приложение доступно по адресу:
```
http://YOUR_SERVER_IP       (порт 80)
http://YOUR_SERVER_IP:8080  (если APP_PORT=8080)
```

---

## 5. Управление данными (xlsx)

Все данные хранятся в Docker volume `forgefit_data` → `/app/data/` внутри контейнера.

Структура:
```
/app/data/
├── _users.xlsx              # реестр всех пользователей
├── {userId-1}.xlsx          # данные пользователя 1
│     Sheet "_Exercises"     # каталог упражнений
│     Sheet "_Sessions"      # список тренировок
│     Sheet "Bench Press"    # подходы (одна строка = один подход)
│     Sheet "Squat"
└── {userId-2}.xlsx          # данные пользователя 2
```

### Резервное копирование

```bash
# Создать архив
docker run --rm \
  -v forgefit_forgefit_data:/data \
  -v $(pwd):/backup \
  alpine tar czf /backup/forgefit-backup-$(date +%Y%m%d).tar.gz -C /data .

# Восстановить
docker run --rm \
  -v forgefit_forgefit_data:/data \
  -v $(pwd):/backup \
  alpine tar xzf /backup/forgefit-backup-YYYYMMDD.tar.gz -C /data
```

---

## 6. Управление контейнерами

```bash
docker compose ps                  # статус
docker compose logs -f             # все логи
docker compose logs -f server      # логи бэкенда
docker compose logs -f frontend    # логи фронтенда
docker compose stop                # остановить
docker compose down                # удалить контейнеры (данные в volume сохраняются)
docker compose down -v             # удалить всё вместе с данными (ОСТОРОЖНО!)
docker compose up -d --build       # пересобрать и перезапустить
```

---

## 7. Регистрация первого пользователя

Откройте `http://YOUR_SERVER_IP` в браузере и зарегистрируйтесь через форму.

Через curl:
```bash
curl -X POST http://YOUR_SERVER_IP/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"securepassword","name":"Admin"}'
```

---

## 8. HTTPS с Certbot (если есть домен)

```bash
sudo apt install -y nginx certbot python3-certbot-nginx

sudo tee /etc/nginx/sites-available/forgefit << 'EOF'
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
EOF

sudo ln -s /etc/nginx/sites-available/forgefit /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx
sudo certbot --nginx -d your-domain.com -d www.your-domain.com
```

После сертификата обновите `.env` и пересоберите:
```env
CORS_ORIGIN=https://your-domain.com
APP_PORT=8080
```
```bash
docker compose up -d --build
```

---

## 9. Автозапуск при перезагрузке

```bash
sudo systemctl enable docker
```

Политика `restart: unless-stopped` в `docker-compose.yml` обеспечивает автоперезапуск контейнеров.

---

## 10. Обновление приложения

```bash
cd /home/user/forgefit
git pull
docker compose up -d --build
```

Данные в volume **не затрагиваются** при пересборке образов.

---

## 11. Переход на другую СУБД

Для перехода на PostgreSQL/SQLite/MongoDB:

1. Реализуйте интерфейс `IDatabase` (`server/Forgefit.Api/Database/IDatabase.cs`):

```csharp
// server/Forgefit.Api/Database/PostgresDatabase.cs
public class PostgresDatabase : IDatabase
{
    // реализуйте все методы через Npgsql / Dapper / EF Core
}
```

2. Замените регистрацию в `Program.cs`:

```csharp
// вместо XlsxDatabase:
var connStr = Environment.GetEnvironmentVariable("DATABASE_URL")!;
builder.Services.AddSingleton<IDatabase>(new PostgresDatabase(connStr));
```

3. Добавьте в `.env`:
```env
DB_TYPE=postgres
DATABASE_URL=Host=db;Database=forgefit;Username=user;Password=pass
```

Рекомендуемые NuGet-пакеты:
| СУБД | Пакет |
|---|---|
| PostgreSQL | `Npgsql` + `Dapper` |
| SQLite | `Microsoft.EntityFrameworkCore.Sqlite` |
| MongoDB | `MongoDB.Driver` |

---

## API-справочник

| Метод | URL | Описание | Auth |
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
