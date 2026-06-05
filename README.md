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
- osnovni upis ocitavanja i stanja senzora
- alarmi i osnovna detekcija neaktivnih senzora

## Pokretanje

Komande ce biti dopunjene kada se doda prva funkcionalna verzija API-ja i klijenta.
