# SensorClient

Konzolna aplikacija koja simulira senzore i salje ocitavanja ka IngestionService API-ju.

Trenutno pokrece 5 senzora. Svaki senzor:

- ima svoj `SensorId`
- ima svoj redni `MessageId`
- generise temperaturu iz zadatog opsega
- racuna alarm na osnovu tri granice
- salje ocitavanje na svakih 1-10 sekundi

Alarmi se ispisuju bojom u konzoli:

- prioritet 1: zuta
- prioritet 2: narandzasta
- prioritet 3: crvena

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

Za zaustavljanje koristiti `Ctrl+C`.
