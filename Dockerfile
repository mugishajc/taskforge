FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Tasks.Api.sln .
COPY src/Tasks.Api/Tasks.Api.csproj src/Tasks.Api/
COPY tests/Tasks.Api.Tests/Tasks.Api.Tests.csproj tests/Tasks.Api.Tests/
RUN dotnet restore

COPY . .
RUN dotnet publish src/Tasks.Api/Tasks.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Tasks.Api.dll"]
