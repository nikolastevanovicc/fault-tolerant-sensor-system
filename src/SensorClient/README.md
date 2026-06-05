# SensorClient

Konzolna aplikacija koja simulira senzore i salje ocitavanja ka IngestionService API-ju.

## Pokretanje

Prvo pokrenuti `IngestionService`, zatim:

```bash
dotnet run --project src/SensorClient/SensorClient.csproj
```

Podrazumevana adresa servera je `http://localhost:5095`.

Druga adresa moze se proslediti kao argument:

```bash
dotnet run --project src/SensorClient/SensorClient.csproj -- http://localhost:5095
```
