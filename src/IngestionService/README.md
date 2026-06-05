# IngestionService

ASP.NET Core Web API servis za prijem, validaciju i cuvanje ocitavanja senzora.

## Endpoint-i

- `GET /health` - osnovna provera dostupnosti servisa
- `POST /api/readings` - prijem jednog ocitavanja senzora

Primer zahteva nalazi se u `IngestionService.http`.
