# Shared

Zajednicki projekat za DTO modele, enum-e i ugovore izmedju servisa.

## Modeli

- `DataQuality` - kvalitet podataka senzora: `Good`, `Bad`, `Uncertain`
- `AlarmPriority` - prioritet alarma od `None` do `High`
- `SensorReadingDto` - jedno ocitavanje senzora koje klijent salje serveru
- `SensorStateDto` - trenutno poznato stanje senzora
- `IngestReadingResponseDto` - odgovor servera nakon prijema ocitavanja
