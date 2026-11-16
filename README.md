# DistributedWMS

System zarządzania gospodarką magazynową w strukturze rozproszonej (magazyn główny - magazyny mobilne).

Praca dyplomowa inżynierska, Arkadiusz Szczęsny, 2026.

## Zakres funkcjonalny

- Kartoteka towarowa z progami stanów minimalnych
- Lokalizacje magazynowe: magazyn główny oraz samochody serwisowe
- Dokumenty magazynowe: PZ (przyjęcie zewnętrzne), MM (przesunięcie międzymagazynowe), RW (rozchód wewnętrzny), IN (inwentaryzacja)
- Transakcyjne księgowanie dokumentów z kontrolą dostępności towaru
- Podgląd stanów w lokalizacji oraz rozmieszczenia towaru w systemie
- Raport braków względem stanów minimalnych
- Uwierzytelnianie oparte na tokenach JWT, autoryzacja rolowa
- Zarządzanie kontami pracowników i przypisaniem magazynów mobilnych

## Zapewnienie spójności danych

Ochrona poprawności stanów magazynowych realizowana jest na czterech niezależnych poziomach:

| Poziom | Mechanizm |
| --- | --- |
| Model domenowy | metoda `StockItem.Decrease` odrzuca rozchód przekraczający stan |
| Warstwa aplikacji | transakcja obejmująca wszystkie zmiany dokumentu |
| Współbieżność | blokada optymistyczna na kolumnie systemowej `xmin` |
| Baza danych | ograniczenie `CHECK ("Quantity" >= 0)` |

## Wykorzystane technologie

| Warstwa | Technologia |
| --- | --- |
| Backend | .NET 10, ASP.NET Core Web API |
| Architektura | Clean Architecture (Domain, Application, Infrastructure, WebApi) |
| Dostęp do danych | Entity Framework Core 10, podejście Code-First |
| Baza danych | PostgreSQL 17 |
| Frontend | Blazor WebAssembly, MudBlazor |
| Dokumentacja API | Scalar |
| Testy | xUnit, FluentAssertions |
| Środowisko | Docker Compose |

## Struktura repozytorium

```

DistributedWMS/
|── docker-compose.yml
|── src/
|	|── DistributedWMS.Domain/			encje i reguły biznesowe
|	|── DistributedWMS.Application/			przypadki użycia, kontrakty serwisów
|	|── DistributedWMS.Infrastructure/		EF Core, uwierzytelnianie
|	|── DistributedWMS.Shared/			kontrakty DTO wspólne dla API i klienta
|	|── DistributedWMS.WebApi/			kontrolery REST
|	└── DistributedWMS.Client/			aplikacja Blazor WebAssembly
|── tests/
	└── DistributedWMS.Tests/			testy jednostkowe
```

## Uruchomienie

Wymagania: .NET 10 SDK, Docker Desktop, Visual Studio.

1. Uruchom bazę danych:

	```
	docker compose up -d
	```

2. Otwórz `DistributedWMS.sln` w Visual Studio.

3. Ustaw wiele projektów startowych (`DistributedWMS.WebApi`
	oraz `DistributedWMS.Client`) i uruchom klawiszem F5.

Migracje bazy danych oraz dane początkowe stosowane są automatycznie
przy pierwszym uruchomieniu aplikacji.

- Aplikacja: 'https://localhost:7172'
- Dokumentacja API: 'https://localhost:7181/scalar/v1;

## Konta demonstracyjne

| Login | Hasło | Rola |
| --- | --- | --- |
| kierownik | Haslo123! | Kierownik |
| magazynier | Haslo123! | Magazynier |
| serwisant1 | Haslo123! | Serwisant (AUTO-01)
| serwisant2 | Haslo123! | Serwisant (AUTO-02)

## Kierunki dalszego rozwoju

- Anulowanie dokumentów zaksięgowanych poprzez dokumenty korygujące
- Ewidencja partii i numerów seryjnych
- Integracja uwierzytelniania z katalogiem firmowym

## Uwagi dotyczące konfiguracji

Repozytorium zawiera poświadczenia bazy danych oraz klucz podpisu tokenów w plikach konfiguracyjnych.
Jest to rozwiązanie przyjęte na potrzeby środowiska deweloperskiego i prezentacji projektu. We wdrożeniu
produkcyjnym wartości te należy przenieść do zmiennych środowiskowych lub menedżera sekretów, a polityka
CORS powinna wskazywać konkretny adres aplikacji klienckiej zamiast zezwalać na dowolne źródło.
