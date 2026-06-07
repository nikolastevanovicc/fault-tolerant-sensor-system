# Konsenzus i detekcija malicioznih senzora

## Cilj

`ConsensusService` je Worker servis koji svake minute izračunava jednu konsenzusnu
vrijednost temperature. Pri svakom pokretanju obrade koristi očitanja iz prethodne
potpuno završene UTC minute, tako da u izračun ne ulaze nepotpuni podaci iz trenutne
minute.

## Podaci koji ulaze u konsenzus

`ConsensusService` čita zapise `SensorReadings` iz baze za interval prethodne potpune
UTC minute (`PeriodStart` je uključen, a `PeriodEnd` nije uključen).

Prije izračuna servis:

- ignoriše zapise kod kojih je `IsConsensusValue == true`;
- koristi samo očitanja kvaliteta `GOOD`;
- dodatno isključuje senzore čiji trenutni `SensorState.Quality` ima vrijednost `BAD`.

## Grupisanje po senzoru

Jedan senzor može poslati više očitanja tokom jedne minute. Kada bi svako očitanje
direktno ulazilo u algoritam, senzori koji šalju češće imali bi veći uticaj na rezultat.

Zato se očitanja grupišu po `SensorId`, a za svaki senzor računa se prosječna
temperatura u obrađenoj minuti. Konsenzusni algoritam zatim koristi tačno jednu
prosječnu vrijednost po senzoru.

## Pojednostavljeni BFT algoritam

Algoritam radi nad prosječnim vrijednostima pojedinačnih senzora:

1. Vrijednosti se sortiraju od najniže prema najvišoj.
2. Ako postoji najmanje 5 senzorskih vrijednosti, uklanjaju se najniža i najviša.
3. Konsenzus je prosjek preostalih vrijednosti.
4. Ako postoje 3 ili 4 vrijednosti, računa se prosjek bez uklanjanja ekstrema.
5. Ako postoje manje od 3 `GOOD` senzorske vrijednosti, konsenzus se za tu minutu ne računa.

Primjer:

```text
Vrijednosti:                         51, 52, 53, 54, 115
Nakon sortiranja i uklanjanja ekstrema:  52, 53, 54
Konsenzus:                           53
```

## Upis rezultata u bazu

Izračunati rezultat upisuje se u tabelu `ConsensusReadings` sa sljedećim podacima:

- `PeriodStart` - početak obrađene UTC minute;
- `PeriodEnd` - kraj obrađene UTC minute;
- `Value` - izračunata konsenzusna temperatura;
- `UsedSensorCount` - broj senzorskih prosjeka korištenih nakon eventualnog uklanjanja ekstrema;
- `RawReadingCount` - ukupan broj sirovih očitanja pronađenih za period;
- `Algorithm = TrimmedMeanBft`;
- `CreatedAt` - UTC vrijeme kreiranja zapisa.

## Sprečavanje duplikata

Kombinacija kolona `PeriodStart` i `PeriodEnd` ima jedinstveni indeks. Prije
izračunavanja `ConsensusService` provjerava da li za isti period već postoji zapis u
`ConsensusReadings`. Ove dvije zaštite sprečavaju nastanak duplih konsenzusnih zapisa.

## Detekcija malicioznih senzora

Nakon izračuna konsenzusa, prosjek svakog senzora poredi se sa konsenzusnom
vrijednošću. Odstupanje je sumnjivo kada važi:

```text
abs(sensorAverage - consensusValue) > 10.0 °C
```

Ako senzor ima 3 uzastopna sumnjiva odstupanja, njegov `SensorState.Quality` postavlja
se na `BAD`. Broj uzastopnih odstupanja, posljednje odstupanje i vrijeme ažuriranja
čuvaju se u tabeli `SensorAnomalyStates`. Očitavanje bez sumnjivog odstupanja resetuje
brojač uzastopnih odstupanja.

`IngestionService` čuva postojeće `BAD` stanje i ne prepisuje ga dolaznim porukama
kvaliteta `GOOD`. Budući konsenzusni izračuni isključuju senzore čiji je trenutni
`SensorState.Quality` postavljen na `BAD`.

## Demo malicioznog senzora

Za demonstraciju je potrebno pokrenuti bazu, primijeniti migracije, pokrenuti servise i
sačekati najmanje 3 potpune minute kako bi maliciozni senzor imao 3 uzastopna sumnjiva
odstupanja.

1. Pokrenuti PostgreSQL:

   ```bash
   docker compose -f docker/docker-compose.yml up -d
   ```

2. Primijeniti migracije:

   ```bash
   dotnet ef database update \
     --project src/Persistence/Persistence.csproj \
     --startup-project src/IngestionService/IngestionService.csproj
   ```

3. Pokrenuti `IngestionService`:

   ```bash
   dotnet run --project src/IngestionService/IngestionService.csproj
   ```

4. Pokrenuti `SensorClient` u režimu malicioznog senzora:

   ```bash
   dotnet run --project src/SensorClient/SensorClient.csproj -- --malicious-demo --malicious-offset 60
   ```

5. Pokrenuti `ConsensusService`:

   ```bash
   dotnet run --project src/ConsensusService/ConsensusService.csproj
   ```

6. Sačekati najmanje 3 potpune minute i zatim provjeriti rezultate.

## SQL provjere

Posljednji konsenzusni rezultati:

```sql
SELECT "PeriodStart", "PeriodEnd", "Value", "UsedSensorCount", "RawReadingCount", "Algorithm", "CreatedAt"
FROM "ConsensusReadings"
ORDER BY "CreatedAt" DESC
LIMIT 10;
```

Trenutno stanje senzora:

```sql
SELECT "SensorId", "Quality", "LastMessageTime", "IsActive"
FROM "SensorStates"
ORDER BY "SensorId";
```

Stanje detekcije anomalija:

```sql
SELECT "SensorId", "ConsecutiveDeviationCount", "LastDeviation", "LastUpdatedAt"
FROM "SensorAnomalyStates"
ORDER BY "SensorId";
```

## Report endpoint-i

Izvještajni endpoint-i dostupni su kroz `IngestionService`:

- `GET /api/readings/history` - historija senzorskih očitanja;
- `GET /api/consensus/latest` - posljednja izračunata konsenzusna vrijednost;
- `GET /api/consensus/history` - historija konsenzusnih vrijednosti;
- `GET /api/sensors/active` - trenutno aktivni senzori iz baze.

Primjeri poziva:

```bash
BASE_URL=http://localhost:5095

curl "$BASE_URL/api/readings/history?maxResults=20"
curl "$BASE_URL/api/consensus/latest"
curl "$BASE_URL/api/consensus/history?maxResults=20"
curl "$BASE_URL/api/sensors/active"
```

Endpoint-i historije podržavaju dodatne filtere. `GET /api/readings/history` podržava
`sensorId`, `from`, `to`, `includeConsensus` i `maxResults`, dok
`GET /api/consensus/history` podržava `from`, `to` i `maxResults`.

## Ograničenja implementacije

- Ovo je pojednostavljeni BFT pristup, a ne potpuna PBFT implementacija.
- Algoritam pretpostavlja najmanje 3 `GOOD` senzora za smislen konsenzus.
- Sa tačno 5 senzora, uklanjanje jedne najniže i jedne najviše vrijednosti štiti od jedne ekstremne maliciozne vrijednosti.
- Napredniji produkcijski sistem zahtijevao bi jači identitet senzora, kriptografsku verifikaciju, distribuiranu replikaciju i strožije modele kvarova.
