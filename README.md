# SNUS Sensor Monitoring

Distribuirani sistem za prikupljanje, obradu i cuvanje podataka sa senzora temperature.

## Struktura

```text
src/
  Shared/              DTO modeli, enum-i i ugovori koje dele svi servisi
  SensorClient/        Konzolna aplikacija koja simulira senzore
  IngestionService/    ASP.NET Core Web API za prijem ocitavanja

docker/                docker-compose i lokalna infrastruktura
k8s/                   Kubernetes/Minikube manifesti
docs/                  Arhitektura, bezbednost, konsenzus i screenshots
```

## Trenutni fokus

Prvi deo projekta radi Student 1:

- solution i struktura repozitorijuma
- Shared modeli i enum-i
- SensorClient za najmanje 5 senzora
- IngestionService endpoint `POST /api/readings`
- PostgreSQL upis ocitavanja i stanja senzora
- alarmi, blokiranje i osnovna detekcija neaktivnih senzora

## Pokretanje

Preduslovi:

- .NET SDK
- Docker Desktop

Pokretanje PostgreSQL baze:

```bash
docker compose -f docker/docker-compose.yml up -d
```

Pokretanje servera:

```bash
dotnet run --project src/IngestionService/IngestionService.csproj --launch-profile http
```

Server koristi adresu:

```text
http://localhost:5095
```

Pokretanje klijenta sa 5 simuliranih senzora:

```bash
dotnet run --project src/SensorClient/SensorClient.csproj
```

Zaustavljanje klijenta:

```text
Ctrl+C
```

## API rute

```text
GET  /health
POST /api/readings
GET  /api/sensors
POST /api/sensors/{sensorId}/block
```

`POST /api/readings` prima jedno ocitavanje senzora, validira ga, upisuje u bazu i azurira stanje senzora.

`GET /api/sensors` vraca sve poznate senzore. Senzor je aktivan ako je server primio njegovu poruku u poslednjih 10 sekundi.

`POST /api/sensors/{sensorId}/block` blokira poznat senzor na 30 sekundi. Dok je blokiran, server odbija njegova ocitavanja sa `423 Locked`.

## Baza

Podrazumevani connection string:

```text
Host=localhost;Port=5432;Database=snus_sensor_monitoring;Username=snus;Password=snus
```

Tabele koje se trenutno koriste:

```text
SensorReadings
SensorStates
```

Servis pri pokretanju koristi `EnsureCreated` da napravi tabele ako ne postoje.

Provera podataka u bazi:

```bash
docker compose -f docker/docker-compose.yml exec -T postgres psql -U snus -d snus_sensor_monitoring -c 'select count(*) from "SensorReadings";'
```

## Demo scenario

1. Pokrenuti PostgreSQL.
2. Pokrenuti `IngestionService`.
3. Pokrenuti `SensorClient`.
4. Proveriti da server prima `HTTP 200` odgovore za senzore.
5. Otvoriti `GET /api/sensors` i proveriti da postoji 5 senzora.
6. Blokirati senzor preko `POST /api/sensors/sensor-001/block`.
7. Proveriti da blokirani senzor privremeno dobija `423 Locked`.
