.PHONY: build test run clean docker-up docker-down restore

restore:
	dotnet restore

build: restore
	dotnet build --no-restore

test: build
	dotnet test --no-build --verbosity normal

run:
	dotnet run --project src/Tasks.Api

watch:
	dotnet watch run --project src/Tasks.Api

docker-up:
	docker compose up --build -d

docker-down:
	docker compose down

docker-logs:
	docker compose logs -f api

clean:
	dotnet clean
	rm -rf src/Tasks.Api/bin src/Tasks.Api/obj
	rm -rf tests/Tasks.Api.Tests/bin tests/Tasks.Api.Tests/obj
