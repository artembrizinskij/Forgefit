# Развёртывание Forgefit на Linux-сервере

## Архитектура

```
GitHub Actions (push → master)
        │
        ▼
  Build Docker images
  Push → GHCR (ghcr.io)
        │
        ▼ SSH
┌───────────────────────────────────────────┐
│           Linux Server (Docker)           │
│                                           │
│  ┌─────────────┐    ┌──────────────────┐  │
│  │  frontend   │    │     server       │  │
│  │ nginx :80   │───▶│ ASP.NET Core     │  │
│  │ Vue 3 SPA   │    │ C# API :3000     │  │
│  └─────────────┘    └────────┬─────────┘  │
│                              │            │
│                     volume: forgefit_data │
│                     /app/data/*.xlsx      │
└───────────────────────────────────────────┘
          │ порт 80 (или APP_PORT)
     Браузер / Интернет
```

- **frontend** — nginx раздаёт Vue 3 SPA и проксирует `/api/` на бэкенд
- **server** — ASP.NET Core 8 Web API (C#), работает внутри сети Docker
- **xlsx-база** — каждый пользователь = отдельный `.xlsx` файл в Docker volume

---

## Часть 1 — Первоначальная настройка сервера

### 1.1 Требования

| Компонент | Минимум |
|---|---|
| ОС | Ubuntu 22.04 / Debian 12 |
| Docker Engine | ≥ 24.0 |
| Docker Compose plugin | ≥ 2.20 |
| RAM | 512 MB |
| Открытый порт | 80 (или любой другой) |

### 1.2 Установка Docker

```bash
sudo apt update && sudo apt upgrade -y
sudo apt install -y ca-certificates curl gnupg

# GPG-ключ и репозиторий Docker
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg \
  | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg

echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
  https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo $VERSION_CODENAME) stable" \
  | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io \
                   docker-buildx-plugin docker-compose-plugin

# Запускать Docker без sudo
sudo usermod -aG docker $USER
newgrp docker

# Убедиться что служба Docker включена (автозапуск при перезагрузке)
sudo systemctl enable docker

# Проверка
docker --version        # Docker version 26.x.x
docker compose version  # Docker Compose version v2.x.x
```

### 1.3 Клонирование репозитория

```bash
git clone https://github.com/artembrizinskij/Forgefit.git /opt/forgefit
cd /opt/forgefit
```

### 1.4 Создание .env файла

```bash
cp .env.example .env
nano /opt/forgefit/.env
```

Минимальный `.env` для продакшена:

```env
# Обязательно — минимум 32 символа для HS256!
JWT_SECRET=ЗАМЕНИТЕ_НА_СЛУЧАЙНУЮ_СТРОКУ

JWT_EXPIRES_IN=7d

# Порт на котором будет доступно приложение
APP_PORT=80

# Ваш домен или IP
CORS_ORIGIN=http://ВАШ_IP_ИЛИ_ДОМЕН
```

Сгенерировать надёжный JWT_SECRET:
```bash
openssl rand -base64 32
```

> ⚠️ Никогда не коммитьте `.env` в репозиторий.

### 1.5 Сборка и первый запуск

```bash
cd /opt/forgefit
docker compose up -d --build
```

Первая сборка занимает 3–5 минут (скачивает образы .NET SDK и Node.js).

```bash
# Проверка статуса
docker compose ps

# Проверка API
curl http://localhost/api/health
# → {"status":"ok","timestamp":"..."}

# Логи в реальном времени
docker compose logs -f
```

Приложение доступно по адресу `http://ВАШ_IP`.

### 1.6 Автозапуск при перезагрузке сервера

Политика `restart: unless-stopped` в `docker-compose.yml` обеспечивает
автоматический перезапуск контейнеров при старте Docker.

Docker уже включён в автозагрузку после шага 1.2. Проверить:
```bash
sudo systemctl is-enabled docker  # → enabled
```

Протестировать:
```bash
sudo reboot
# после перезагрузки:
docker compose -C /opt/forgefit ps  # оба контейнера должны быть Up
```

---

## Часть 2 — CI/CD через GitHub Actions

При каждом пуше в ветку `master` GitHub Actions автоматически:
1. Собирает Docker-образы фронтенда и бэкенда
2. Пушит их в GitHub Container Registry (GHCR)
3. Подключается к серверу по SSH и обновляет контейнеры без downtime

### 2.1 Создание SSH-ключа для деплоя

На **сервере** выполните:
```bash
# Генерируем отдельный ключ (не используйте основной!)
ssh-keygen -t ed25519 -C "forgefit-deploy" -f ~/.ssh/forgefit_deploy -N ""

# Добавляем публичный ключ в authorized_keys
cat ~/.ssh/forgefit_deploy.pub >> ~/.ssh/authorized_keys

# Выводим приватный ключ — он нужен для GitHub Secret
cat ~/.ssh/forgefit_deploy
```

### 2.2 Настройка GitHub Secrets

Перейдите в репозиторий → **Settings → Secrets and variables → Actions → New repository secret**:

| Secret | Описание | Пример |
|---|---|---|
| `DEPLOY_HOST` | IP или домен сервера | `192.168.1.100` |
| `DEPLOY_USER` | SSH пользователь | `ubuntu` |
| `DEPLOY_SSH_KEY` | Содержимое `~/.ssh/forgefit_deploy` (приватный ключ целиком) | `-----BEGIN OPENSSH...` |
| `DEPLOY_PORT` | SSH порт (необязательно) | `22` |
| `DEPLOY_PATH` | Путь на сервере (необязательно) | `/opt/forgefit` |

### 2.3 Как работает деплой

```
push → master
  │
  ├─ [Job: build]  ~3-4 мин
  │    ├─ docker build frontend → ghcr.io/.../forgefit-frontend:abc1234
  │    ├─ docker build server   → ghcr.io/.../forgefit-server:abc1234
  │    └─ docker push (тег SHA + latest)
  │
  └─ [Job: deploy]  ~30 сек
       ├─ scp docker-compose.prod.yml → сервер
       ├─ ssh: docker pull latest
       ├─ ssh: docker compose up -d --no-build
       └─ ssh: docker image prune -f
```

Следить за прогрессом: GitHub → вкладка **Actions**.

### 2.4 Продакшн-compose на сервере

При первом деплое через Actions на сервер копируется `docker-compose.prod.yml`.
Он использует образы из GHCR вместо локальной сборки.

Если нужно запустить вручную без Actions:
```bash
cd /opt/forgefit
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d
```

---

## Часть 3 — HTTPS (если есть домен)

```bash
# Меняем APP_PORT чтобы не конфликтовать с системным nginx
nano /opt/forgefit/.env
# APP_PORT=8080

docker compose up -d

# Устанавливаем Certbot
sudo apt install -y nginx certbot python3-certbot-nginx

# Конфиг nginx как reverse proxy
sudo tee /etc/nginx/sites-available/forgefit << 'EOF'
server {
    listen 80;
    server_name ВАШ_ДОМЕН www.ВАШ_ДОМЕН;
    location / {
        proxy_pass         http://127.0.0.1:8080;
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

# Получаем SSL-сертификат (автоматически обновляется)
sudo certbot --nginx -d ВАШ_ДОМЕН -d www.ВАШ_ДОМЕН
```

Обновите в `.env`:
```env
CORS_ORIGIN=https://ВАШ_ДОМЕН
```
```bash
docker compose up -d
```

---

## Часть 4 — Управление данными

### Структура xlsx-базы

```
/app/data/  (Docker volume: forgefit_data)
├── _users.xlsx              # реестр всех пользователей
├── {userId-1}.xlsx          # данные пользователя 1
│     Sheet "_Exercises"     # каталог упражнений
│     Sheet "_Sessions"      # список тренировок
│     Sheet "Bench Press"    # история подходов
│     Sheet "Squat"
└── {userId-2}.xlsx
```

### Резервное копирование

```bash
mkdir -p /opt/forgefit/backups

# Создать архив
docker run --rm \
  -v forgefit_forgefit_data:/data \
  -v /opt/forgefit/backups:/backup \
  alpine tar czf /backup/forgefit-$(date +%Y%m%d-%H%M).tar.gz -C /data .

# Восстановить (перезапишет данные!)
docker compose down
docker run --rm \
  -v forgefit_forgefit_data:/data \
  -v /opt/forgefit/backups:/backup \
  alpine tar xzf /backup/forgefit-YYYYMMDD-HHMM.tar.gz -C /data
docker compose up -d
```

### Автоматический бэкап (cron)

```bash
crontab -e
```
Добавить (бэкап каждую ночь в 3:00):
```
0 3 * * * docker run --rm -v forgefit_forgefit_data:/data -v /opt/forgefit/backups:/backup alpine tar czf /backup/forgefit-$(date +\%Y\%m\%d).tar.gz -C /data . 2>/dev/null
```

---

## Часть 5 — Полезные команды

```bash
# Статус контейнеров
docker compose -f /opt/forgefit/docker-compose.prod.yml ps

# Логи
docker compose -f /opt/forgefit/docker-compose.prod.yml logs -f
docker compose -f /opt/forgefit/docker-compose.prod.yml logs -f server
docker compose -f /opt/forgefit/docker-compose.prod.yml logs -f frontend

# Перезапустить
docker compose -f /opt/forgefit/docker-compose.prod.yml restart

# Остановить (данные сохраняются в volume)
docker compose -f /opt/forgefit/docker-compose.prod.yml down

# Полный сброс с удалением данных (ОСТОРОЖНО!)
docker compose -f /opt/forgefit/docker-compose.prod.yml down -v

# Ручное обновление без Actions
docker compose -f /opt/forgefit/docker-compose.prod.yml pull
docker compose -f /opt/forgefit/docker-compose.prod.yml up -d
```

---

## Часть 6 — Переход на реальную БД

1. Реализуйте интерфейс `IDatabase` (`server/Forgefit.Api/Database/IDatabase.cs`)
2. Зарегистрируйте в `Program.cs`:
```csharp
builder.Services.AddSingleton<IDatabase>(new PostgresDatabase(connStr));
```
3. Добавьте в `.env`:
```env
DB_TYPE=postgres
DATABASE_URL=Host=db;Database=forgefit;Username=user;Password=pass
```

| СУБД | NuGet пакет |
|---|---|
| PostgreSQL | `Npgsql` + `Dapper` |
| SQLite | `Microsoft.EntityFrameworkCore.Sqlite` |
| MongoDB | `MongoDB.Driver` |

---

## API-справочник

| Метод | URL | Auth |
|---|---|---|
| POST | `/api/auth/register` | — |
| POST | `/api/auth/login` | — |
| GET | `/api/auth/me` | ✅ |
| GET | `/api/exercises` | ✅ |
| POST | `/api/exercises` | ✅ |
| PUT | `/api/exercises/{id}` | ✅ |
| DELETE | `/api/exercises/{id}` | ✅ |
| GET | `/api/workouts/today` | ✅ |
| GET | `/api/workouts/history/{exerciseId}` | ✅ |
| POST | `/api/workouts/sets` | ✅ |
| DELETE | `/api/workouts/sets/{id}` | ✅ |
| GET | `/api/health` | — |

Swagger UI (только Development): `http://localhost:5000/swagger`
