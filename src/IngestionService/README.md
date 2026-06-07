# IngestionService

ASP.NET Core Web API servis za prijem, validaciju i cuvanje ocitavanja senzora.

## Baza

Servis koristi PostgreSQL preko Entity Framework Core-a.

Podrazumevani connection string:

```text
Host=localhost;Port=5432;Database=snus_sensor_monitoring;Username=snus;Password=snus
```

Lokalna PostgreSQL baza moze se pokrenuti iz repozitorijuma:

```bash
docker compose -f docker/docker-compose.yml up -d
```

Pri pokretanju servis primenjuje Entity Framework Core migracije.

## Endpoint-i

- `GET /health` - osnovna provera dostupnosti servisa
- `POST /api/readings` - prijem jednog ocitavanja senzora
- `GET /api/sensors` - pregled svih poznatih senzora i njihovog trenutnog statusa
- `POST /api/sensors/{sensorId}/block` - privremeno blokira poznati senzor na 30 sekundi

Primer zahteva nalazi se u `IngestionService.http`.

Senzor se smatra aktivnim ako je server primio njegovu poruku u poslednjih 10 sekundi.

Ako ocitavanje sadrzi alarm prioriteta 1, 2 ili 3, server ispisuje alarm u konzoli odgovarajucom bojom.

Ako je senzor blokiran, server odbija njegovo ocitavanje dok ne prodje vreme iz `BlockedUntil`.
