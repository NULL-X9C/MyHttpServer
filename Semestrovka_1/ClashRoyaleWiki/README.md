# Clash Royale Cards Wiki 

Веб-приложение (библиотека карт Clash Royale с особыми формами Evolution/Hero и административной панелью), реализованное на собственном HTTP/MVC-фреймворке на C# (.NET 10) без использования ASP.NET Core, Entity Framework и CSS-фреймворков.

---

## Технологический стек

* **Backend:** C# / .NET 10.0, самописный HTTP-движок на `HttpListener` (`MyHttpServer.Framework`), собственный шаблонизатор (`MiniTemplateEngine`), собственная ORM поверх `Npgsql` (`MyHttpServer.ORM`).
* **База данных:** PostgreSQL 16 (4 связанные таблицы: `cards`, `card_stats`, `card_forms`, `users`).
* **Frontend:** чистый HTML5, адаптивный CSS3 (БЭМ), Vanilla JavaScript (Fetch API), WYSIWYG-редактор **Quill.js** в админ-панели.
* **Инфраструктура:** Docker, Docker Compose (multi-stage build, `healthcheck`, автоматическая инициализация БД).

---

## Структура проекта

```text
Semestrovka_1/ClashRoyaleWiki/
├── ClashRoyaleWiki.slnx          # Файл решения
├── Dockerfile                    # Двухэтапная сборка (.NET 10 SDK -> .NET 10 Runtime)
├── docker-compose.yml            # Оркестрация PostgreSQL 16 и веб-сервера
├── .dockerignore                 # Исключения при сборке образа
├── README.md                     # Инструкция по запуску и работе с сервером
├── sql/
│   └── init.sql                  # DDL (4 таблицы) + начальное заполнение (123 карты, 63 формы)
└── src/
    ├── MiniTemplateEngine/       # Собственный HTML-шаблонизатор (${Prop}, $if, $foreach)
    ├── MyHttpServer.ORM/         # Собственная ORM (CRUD, параметризованные SQL-запросы, enum-маппинг)
    ├── MyHttpServer.Framework/   # HTTP-сервер, Chain of Responsibility, роутинг, DI, Model Binding
    └── MyHttpServer/             # Прикладное веб-приложение:
        ├── Controllers/          # CardsController, ApiController, AuthController, AdminController
        ├── Models/               # Entities/, DTOs/, ViewModels/, Auth/
        ├── Repositories/         # Паттерн Repository (1 таблица = 1 репозиторий)
        ├── Services/             # CardService, AuthService, ValidationService
        ├── Views/                # HTML-шаблоны (Cards, Auth, Admin)
        ├── static/               # CSS (style.css), Vanilla JS, изображения карт, 403.html, 404.html
        └── settings.json         # Конфигурация для локального запуска
```

### Конвейер обработки HTTP-запросов
```text
HttpListener -> ExceptionHandler (500) -> StaticFilesHandler (статика / 403) -> ControllersHandler (MVC / API) -> NotFoundHandler (404)
```

---

## Запуск через Docker (рекомендуемый способ)

Для запуска требуется только установленный и запущенный **Docker** (Docker Desktop на Windows/macOS или Docker Engine на Linux). Устанавливать .NET SDK и PostgreSQL на компьютер не нужно.

### 1. Первый запуск
1. Клонируйте репозиторий и перейдите в папку проекта:
   ```bash
   git clone [https://github.com/NULL-X9C/MyHttpServer.git](https://github.com/NULL-X9C/MyHttpServer.git)
   cd MyHttpServer/Semestrovka_1/ClashRoyaleWiki
   ```
2. Соберите и запустите контейнеры:
   ```bash
   docker compose up --build
   ```
   При первом запуске автоматически произойдёт следующее:
   * поднимется контейнер `clash_royale_db` (PostgreSQL 16) и выполнит `sql/init.sql` (создание 4 таблиц и загрузка 123 карт, характеристик и 63 форм Evolution/Hero);
   * контейнер `clash_royale_app` дождётся готовности базы (`healthcheck`), соберёт и запустит C#-сервер на порту `8080`;
   * при старте сервера автоматически создастся учётная запись администратора по умолчанию (`EnsureDefaultAdminAsync`).
3. После появления в логах строки `Prefix: http://+:8080/` откройте в браузере:
   * **Каталог карт:** `http://localhost:8080/cards`
   * **Вход в админ-панель:** `http://localhost:8080/login`
   * **JSON API каталога:** `http://localhost:8080/api/cards`

> **Как работает сохранение данных в Docker:** скрипт `sql/init.sql` выполняется **только один раз** при создании пустого тома `pg_data`. При обычных остановках и перезапусках все добавленные или отредактированные через админку карты сохраняются.

### 2. Основные команды управления Docker

Все команды выполняются из папки `Semestrovka_1/ClashRoyaleWiki`:

| Задача | Команда | Что происходит |
| :--- | :--- | :--- |
| **Запуск / пересборка после правок кода** | `docker compose up --build` | Пересобирает приложение и запускает контейнеры (данные БД сохраняются) |
| **Запуск в фоновом режиме** | `docker compose up -d` | Запускает контейнеры в фоне, не блокируя терминал |
| **Остановить без удаления контейнеров** | `Ctrl + C` или `docker compose stop` | Приостанавливает контейнеры, данные БД сохраняются |
| **Возобновить после `stop`** | `docker compose start` | Быстро запускает остановленные контейнеры без пересборки |
| **Остановить и удалить контейнеры** | `docker compose down` | Удаляет контейнеры и сеть; том БД (`pg_data`) сохраняется |
| **Полный сброс БД (перезалить `init.sql`)** | `docker compose down -v` | Удаляет контейнеры и том БД; при следующем `up` заново выполнится `sql/init.sql` |
| **Полная очистка (контейнеры + БД + образ)** | `docker compose down -v --rmi local` | Удаляет все Docker-артефакты проекта (исходный код на диске не затрагивается) |
| **Проверить запущенные контейнеры** | `docker ps` | Показывает статус `clash_royale_app` и `clash_royale_db` |

### 3. Подключение к Docker-БД извне (опционально)
Для просмотра таблиц через Rider Database, DataGrip или pgAdmin порт базы проброшен на `5433` (чтобы не конфликтовать с локальным PostgreSQL на `5432`):
* **Host:** `localhost`
* **Port:** `5433`
* **Database:** `clash_royale`
* **Username / Password:** `postgres` / `postgres`

---

## Локальный запуск без Docker (для разработки)

Требуются установленные **.NET 10 SDK** и **PostgreSQL 16**.

1. Создайте пустую базу данных `clash_royale` в локальном PostgreSQL и выполните скрипт `sql/init.sql`.
2. Проверьте параметры в `src/MyHttpServer/settings.json`:
   ```json
   {
     "Server": {
       "Host": "127.0.0.1",
       "Port": "1488",
       "Path": "/"
     },
     "Database": {
       "ConnectionString": "Host=localhost;Port=5432;Database=clash_royale;Username=postgres;Password=postgres"
     }
   }
   ```
   *(При запуске в Docker эти поля автоматически переопределяются переменными окружения `SERVER_HOST`, `SERVER_PORT` и `DB_CONNECTION_STRING`.)*
3. Запустите сервер из папки `Semestrovka_1/ClashRoyaleWiki`:
   ```bash
   dotnet run --project src/MyHttpServer/MyHttpServer.csproj
   ```
4. Откройте в браузере `http://127.0.0.1:1488/cards`.
5. **Остановка сервера при локальном запуске:** введите в консоли команду `exit` и нажмите `Enter` (или используйте `Ctrl + C`).

---

## Работа с сервером и функциональность

### Публичная часть
* **Каталог карт (`/cards`):** адаптивная сетка с 3D-переворотом карточек, отображением редкости, стоимости эликсира и мини-плашек особых форм (`Hero`, `Evo`).
* **Живая AJAX-фильтрация (`/api/cards`):** поиск по названию с задержкой (debounce 300 мс) и выпадающим автодополнением, фильтры по редкости, наличию особых форм и стоимости эликсира обновляют сетку без перезагрузки страницы. На мобильных экранах доступна выдвижная шторка фильтров.
* **Детальная страница карты (`/cards/details?slug=...`):** 8 характеристик для Wordle, описание, экспертный разбор стратегии и интерактивные вкладки переключения между базовой версией, Героем и Эволюцией без перезагрузки страницы.

### Авторизация и Административная панель
При первом запуске сервер автоматически создаёт администратора с солёным PBKDF2-SHA256 хэшем пароля (если пользователь ещё отсутствует в таблице `users`):

* **Страница входа:** `/login`
* **Email:** `admin@clash.ru`
* **Пароль:** `admin123`

После входа выдаётся `HttpOnly`-cookie сессии (`clash_session`) и открывается доступ к админ-панели (`/admin`):
* **Просмотр, создание, редактирование и удаление карт** (одновременное сохранение данных в таблицах `cards` и `card_stats`).
* **Загрузка изображений с ПК:** поддерживаются форматы `.png`, `.webp`, `.jpg`, `.jpeg` размером до 5 МБ (предпросмотр через `FileReader`, передача в Base64 и сохранение в `static/images/cards/`).
* **WYSIWYG-редактор Quill.js:** форматирование экспертной стратегии карты с ограничением длины текста.
* **Управление формами Evolution / Hero:** добавление особых форм (с живым счётчиком `0 / 300` символов) на странице карты и их удаление в режиме редактирования карты.
* **Двойная валидация:** все формы проверяются как на клиенте (Vanilla JS), так и на сервере (`ValidationService`), включая регулярные выражения для Email и URL `slug`.

### Обработка ошибок и безопасность
* **Несуществующие маршруты и карты:** возвращают HTTP `404` со страницей `static/404.html` и ссылкой возврата в каталог.
* **Защита от Path Traversal:** попытки выйти за пределы папки `static/` (например, `/%2e%2e/settings.json`) блокируются со статусом `403 Forbidden`.
* **Устойчивость к сбоям:** `ExceptionHandler` перехватывает непредвиденные исключения, отдаёт страницу `500` со ссылкой на `/cards`, не останавливая работу сервера.
* **Защита от SQL-инъекций:** все запросы к PostgreSQL в `MyHttpServer.ORM` параметризованы.

---

## Карта маршрутов сервера (14 эндпоинтов)

| Метод | Маршрут | Описание | Доступ |
| :--- | :--- | :--- | :--- |
| `GET` | `/` | Редирект на `/cards` | Все |
| `GET` | `/cards` | Каталог карт (SSR + поддержка серверной фильтрации) | Все |
| `GET` | `/cards/details?slug=...` | Детальная страница карты, характеристик и форм | Все |
| `GET` | `/api/cards` | JSON API для AJAX-поиска и фильтрации каталога | Все |
| `GET` | `/login` | Страница входа в админ-панель | Все |
| `POST` | `/login` | Проверка учётных данных и установка `HttpOnly` cookie | Все |
| `POST` | `/logout` | Завершение сессии и очистка cookie | Все |
| `GET` | `/admin` | Таблица всех карт в админ-панели | Admin |
| `GET` | `/admin/cards/create` | Форма создания новой карты | Admin |
| `GET` | `/admin/cards/edit?id=...` | Форма редактирования карты, характеристик и списка форм | Admin |
| `POST` | `/admin/cards/save` | Создание или обновление карты, статов и картинки | Admin |
| `POST` | `/admin/cards/delete` | Удаление карты (каскадно удаляет статы и формы) | Admin |
| `POST` | `/admin/forms/add` | Добавление особой формы (`Evolution` / `Hero`) | Admin |
| `POST` | `/admin/forms/delete` | Удаление особой формы | Admin |
