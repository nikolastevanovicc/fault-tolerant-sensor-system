# IngestionService

ASP.NET Core Web API servis za prijem, validaciju i cuvanje ocitavanja senzora.

## Endpoint-i

- `GET /health` - osnovna provera dostupnosti servisa
- `POST /api/readings` - prijem jednog ocitavanja senzora
- `GET /api/sensors` - pregled svih poznatih senzora i njihovog trenutnog statusa

Primer zahteva nalazi se u `IngestionService.http`.

Senzor se smatra aktivnim ako je server primio njegovu poruku u poslednjih 10 sekundi.

Ako ocitavanje sadrzi alarm prioriteta 1, 2 ili 3, server ispisuje alarm u konzoli odgovarajucom bojom.
