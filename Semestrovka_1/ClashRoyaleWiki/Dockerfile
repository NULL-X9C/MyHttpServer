# Этап 1: сборка и публикация проекта
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем только .csproj для кэширования слоя NuGet-пакетов
COPY ["src/MyHttpServer/MyHttpServer.csproj", "src/MyHttpServer/"]
COPY ["src/MyHttpServer.Framework/MyHttpServer.Framework.csproj", "src/MyHttpServer.Framework/"]
COPY ["src/MyHttpServer.ORM/MyHttpServer.ORM.csproj", "src/MyHttpServer.ORM/"]
COPY ["src/MiniTemplateEngine/MiniTemplateEngine.csproj", "src/MiniTemplateEngine/"]

RUN dotnet restore "src/MyHttpServer/MyHttpServer.csproj"

# Копируем исходники и публикуем релизную сборку
COPY src/ src/
RUN dotnet publish "src/MyHttpServer/MyHttpServer.csproj" -c Release -o /app/publish --no-restore

# Этап 2: минимальный образ для запуска (.NET 10 Runtime)
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "MyHttpServer.dll"]