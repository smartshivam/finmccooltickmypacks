# Этап сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Копируем проект и восстанавливаем зависимости
COPY ["MyToursApi.csproj", "./"]
RUN dotnet restore "MyToursApi.csproj"
# Копируем исходный код и публикуем приложение
COPY . .
RUN dotnet publish "MyToursApi.csproj" -c Release -o /app/publish

# Этап выполнения (runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
# Копируем опубликованные файлы из этапа сборки
COPY --from=build /app/publish .

# Устанавливаем EF CLI tool, чтобы можно было применять миграции
RUN dotnet tool install --global dotnet-ef
# Добавляем глобальные инструменты в PATH
ENV PATH="${PATH}:/root/.dotnet/tools"

# Копируем entrypoint-скрипт в контейнер и делаем его исполняемым
COPY entrypoint.sh .
RUN chmod +x entrypoint.sh

ENTRYPOINT ["./entrypoint.sh"]
