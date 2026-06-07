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

Za demonstraciju detekcije zlonamernog senzora:

```bash
dotnet run --project src/SensorClient/SensorClient.csproj -- --malicious-demo
```

Podrazumevano se menja vrednost poslednjeg senzora (`sensor-005`) za `60.0` stepeni. Senzor i
odstupanje mogu se podesiti argumentima:

```bash
dotnet run --project src/SensorClient/SensorClient.csproj -- --malicious-demo --malicious-sensor sensor-004 --malicious-offset 45.5
```

Za zaustavljanje koristiti `Ctrl+C`.
