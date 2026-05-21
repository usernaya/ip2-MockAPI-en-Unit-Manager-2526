# HvacMock Platform — Quick Reference

> Lokaal HVAC mock platform · C# / .NET 8 · DynamoDB Local · Docker

## Over dit project

Dit project is gebouwd voor **Inetum** in het kader van Integration Project 2 (Toegepaste Informatica, 2025–2026).

HVAC staat voor **Heating, Ventilation and Air Conditioning**, toestellen zoals verwarmingssystemen, airco's en ventilatie-units. We hebben een **mock API** gebouwd die zich gedraagt als een echte.

Het platform bestaat uit:
- **MockApi** — simuleert de echte klantgerichte Daikin API (1-op-1 JSON-structuur)
- **AdminApi** — beheertool om devices aan te maken, aan te passen en te verwijderen
- **Gebruikers-UI** — webinterface die de MockApi aanspreekt
- **Admin UI** — webinterface die de AdminApi aanspreekt
- **DynamoDB** — centrale database zodat de state gedeeld is voor alle gebruikers

> Groep 8: Aya, Bunyamine, Hamid, Turhan, Zineddine

---

## Opstarten

```bash
# 1. Database starten
docker compose up -d

# 2. Alle projecten starten (Visual Studio launch profile)
# of apart:
dotnet run --project HvacMock.MockApi
dotnet run --project HvacMock.AdminApi
dotnet run --project HvacMock.UI
dotnet run --project HvacMock.UI.Admin

# 3. Unit tests runnen
dotnet test
# Verwacht: 17 passed, 0 failed
```

---

## Poorten

| Service | URL |
|---|---|
| MockApi | http://localhost:5294 |
| AdminApi | http://localhost:5100 |
| Gebruikers-UI | http://localhost:5200 |
| Admin UI | http://localhost:5201 |
| DynamoDB Local | http://localhost:3131 |
| DynamoDB Admin UI | http://localhost:8001 |
| Swagger (MockApi) | http://localhost:5294/swagger |

---

## Credentials

### MockApi — client credentials
```
client_id:     hvac-client
client_secret: hvac-secret
```
> Bestand: `HvacMock.MockApi/appsettings.json` → sectie `Auth`

### AdminApi — username/password
```
username: admin
password: admin123
```
> Bestand: `HvacMock.AdminApi/Controllers/AuthController.cs` (hardcoded voor demo)

### JWT secrets
```
MockApi secret:  hvac-mock-api-secret-key-2024-minimum-32-chars
AdminApi secret: hvac-admin-demo-secret-sleutel-minimaal-32-tekens
```
> MockApi: `HvacMock.MockApi/appsettings.json` → sectie `Jwt.SecretKey`  
> AdminApi: `HvacMock.AdminApi/appsettings.json` → sectie `Jwt.Secret`

---

## Endpoints

### MockApi — localhost:5294

| Method | Route | Auth | Beschrijving |
|---|---|---|---|
| POST | `/v1/oidc/token` | — | Client credentials → JWT |
| GET | `/v1/gateway-devices` | ✅ | Alle devices ophalen |
| GET | `/v1/gateway-devices/{id}` | ✅ | Één device ophalen |
| PATCH | `/v1/gateway-devices/{id}/{**field}` | ✅ | Één veld aanpassen via URL-pad |

### AdminApi — localhost:5100

| Method | Route | Auth | Beschrijving |
|---|---|---|---|
| POST | `/auth/token` | — | Admin login → JWT |
| GET | `/admin/devices` | ✅ | Alle devices ophalen |
| GET | `/admin/devices/{id}` | ✅ | Één device ophalen |
| POST | `/admin/devices` | ✅ | Nieuw device aanmaken |
| PATCH | `/admin/devices/{id}` | ✅ | Veld aanpassen via body-pad |
| DELETE | `/admin/devices/{id}` | ✅ | Device verwijderen |

---

## PATCH gebruiken

### MockApi — pad in de URL
```http
PATCH /v1/gateway-devices/{deviceId}/{**field}
Content-Type: application/json
Authorization: Bearer <token>

{ "value": <nieuwe waarde> }
```

**Voorbeeld:**
```http
PATCH /v1/gateway-devices/device-1/managementPoints[1].targetTemperature.value

{ "value": 22.5 }
```

### AdminApi — pad in de body
```http
PATCH /admin/devices/{id}
Content-Type: application/json
Authorization: Bearer <token>

{
  "path": "managementPoints[1].targetTemperature.value",
  "value": 22.5
}
```

---

## Alle PATCH-paden

### Basis velden (managementPoints[1] = climateControl)

```
managementPoints[1].onOffMode.value
managementPoints[1].operationMode.value
managementPoints[1].targetTemperature.value
managementPoints[1].name.value
```

### Setpoints per operationMode

```
managementPoints[1].temperatureControl.value.operationModes.heating.setpoints.roomTemperature.value
managementPoints[1].temperatureControl.value.operationModes.cooling.setpoints.roomTemperature.value
managementPoints[1].temperatureControl.value.operationModes.auto.setpoints.roomTemperature.value
```

### Toegestane waarden per veld

| Veld | Type | Toegestane waarden / grenzen |
|---|---|---|
| `onOffMode.value` | string | `"on"`, `"off"` |
| `operationMode.value` | string | `"heating"`, `"cooling"`, `"auto"` |
| `targetTemperature.value` | number | min: 12, max: 30, step: 0.5 |
| `roomTemperature.value` (setpoint) | number | min: 12, max: 30, step: 0.5 |
| `name.value` | string | max. 50 tekens |

> Let op: Als `settable: false` → PATCH geeft `400 Bad Request`
> Let op: Als waarde buiten min/max/step → `400 Bad Request`
> Let op: Als waarde niet in `values` lijst → `400 Bad Request`

---

## Geseedde devices

| ID | Model | Type |
|---|---|---|
| device-1 | Altherma | heating |
| device-2 | FTXM35R | airConditioner |
| device-3 | Daikin Stylish | heatPump |
| device-4 | Perfera | airConditioner |
| device-5 | SkyAir | commercialHVAC |
| device-6 | Emura | heatPump |
| device-7 | Ururu Sarara | airConditioner |
| device-8 | VRV IV | commercialHVAC |

> Seeddata: `HvacMock.MockApi/Infrastructure/seed-devices.json`  
> De tabel wordt automatisch aangemaakt en geseed bij de eerste opstart als de tabel leeg is.

---

## Device JSON-structuur

```json
{
  "id": "device-1",
  "type": "heating",
  "deviceModel": "Altherma",
  "managementPoints": [
    {
      "embeddedId": "0",
      "managementPointType": "gateway",
      "managementPointCategory": "primary",
      "name": {
        "settable": true,
        "value": "Gateway",
        "maxLength": 50
      }
    },
    {
      "embeddedId": "1",
      "managementPointType": "climateControl",
      "managementPointCategory": "primary",
      "onOffMode": {
        "settable": true,
        "values": ["on", "off"],
        "value": "on"
      },
      "operationMode": {
        "settable": true,
        "values": ["heating", "cooling", "auto"],
        "value": "heating"
      },
      "targetTemperature": {
        "settable": true,
        "value": 21.0,
        "minValue": 12.0,
        "maxValue": 30.0,
        "stepValue": 0.5
      },
      "temperatureControl": {
        "settable": true,
        "value": {
          "operationModes": {
            "heating": {
              "setpoints": {
                "roomTemperature": {
                  "settable": true,
                  "value": 21.0,
                  "minValue": 12.0,
                  "maxValue": 30.0,
                  "stepValue": 0.5
                }
              }
            },
            "cooling": {
              "setpoints": {
                "roomTemperature": {
                  "settable": true,
                  "value": 24.0,
                  "minValue": 18.0,
                  "maxValue": 30.0,
                  "stepValue": 0.5
                }
              }
            }
          }
        }
      }
    }
  ]
}
```

---

## Token ophalen (Postman / curl)

### MockApi token
```bash
curl -X POST http://localhost:5294/v1/oidc/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=hvac-client&client_secret=hvac-secret"
```

### AdminApi token
```bash
curl -X POST http://localhost:5100/auth/token \
  -H "Content-Type: application/json" \
  -d '{"username": "admin", "password": "admin123"}'
```

### PATCH uitvoeren
```bash
curl -X PATCH \
  "http://localhost:5294/v1/gateway-devices/device-1/managementPoints[1].targetTemperature.value" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"value": 22.5}'
```

---

## Configuratie aanpassen

### DynamoDB URL wijzigen
```json
// HvacMock.MockApi/appsettings.json
// HvacMock.AdminApi/appsettings.json
{
  "DynamoDb": {
    "ServiceURL": "http://localhost:3131",
    "Region": "eu-west-1"
  }
}
```

### JWT geldigheid wijzigen
```json
// HvacMock.MockApi/appsettings.json
{
  "Jwt": {
    "ExpiresInSeconds": 3600
  }
}

// HvacMock.AdminApi/appsettings.json
{
  "Jwt": {
    "ExpiresInMinutes": "60"
  }
}
```

### MockApi URL wijzigen in de UI
```json
// HvacMock.UI/appsettings.json
{
  "MockApi": {
    "BaseUrl": "http://localhost:5294/v1",
    "ClientId": "hvac-client",
    "ClientSecret": "hvac-secret"
  }
}
```

### AdminApi URL wijzigen in de Admin UI
```json
// HvacMock.UI.Admin/appsettings.json
{
  "AdminApi": {
    "BaseUrl": "http://localhost:5100"
  }
}
```

---

## Unit tests

```bash
# Alle tests
dotnet test

# Alleen Application tests
dotnet test HvacMock.Application.Tests/HvacMock.Application.Tests.csproj

# Met output
dotnet test --logger "console;verbosity=detailed"
```

**Wat wordt getest:**

| Test | Wat |
|---|---|
| GetAllAsync | Lijst van devices ophalen |
| GetByIdAsync | Eén device ophalen op id |
| PatchAsync — geldig veld | targetTemperature correct aanpassen |
| PatchAsync — onbestaand device | KeyNotFoundException |
| PatchAsync — leeg pad | ArgumentException |
| PatchAsync — ongeldig pad | ArgumentException |
| PatchAsync — settable: false | ArgumentException |
| PatchAsync — ongeldige enum | ArgumentException |
| PatchAsync — boven maxValue | ArgumentException |
| PatchAsync — onder minValue | ArgumentException |
| PatchAsync — buiten stepValue | ArgumentException |
| OperationMode sync | targetTemperature volgt mode-setpoint |
| Setpoint sync | roomTemperature werkt door naar targetTemperature |
| Admin — duplicate id | ArgumentException bij create |
| Admin — delete bestaand | Device correct verwijderd |
| Admin — delete onbestaand | false teruggegeven |
| Admin — PATCH delegatie | Delegeert naar DeviceService |

---

## Architectuur

```
SlnHvacMock/
├── HvacMock.Domain/              ← pure modellen, geen externe deps
│   └── Devices/
│       ├── Device.cs
│       ├── ManagementPoint.cs
│       ├── OnOffModeField.cs
│       ├── OperationModeField.cs
│       ├── TargetTemperatureField.cs
│       ├── TemperatureControlField.cs
│       ├── TemperatureControlValue.cs
│       ├── OperationModeSetpoints.cs
│       ├── Setpoint.cs
│       └── SettableStringValue.cs
│
├── HvacMock.Application/         ← businesslogica + interfaces
│   ├── Repositories/
│   │   └── IDeviceRepository.cs
│   ├── Services/
│   │   ├── DeviceService.cs
│   │   └── AdminDeviceService.cs
│   └── Patching/
│       ├── DevicePatchNavigator.cs
│       ├── PatchFieldValidator.cs
│       └── OperationModeTemperatureUpdater.cs
│
├── HvacMock.Infrastructure/      ← DynamoDB implementatie
│   ├── Repositories/
│   │   └── DeviceRepository.cs
│   └── Seeding/
│       └── DataSeeder.cs
│
├── HvacMock.MockApi/             ← publieke API :5294
├── HvacMock.AdminApi/            ← admin API :5100
├── HvacMock.UI/                  ← gebruikers-UI :5200
├── HvacMock.UI.Admin/            ← admin UI :5201
├── HvacMock.Application.Tests/  ← 17 unit tests
└── docker-compose.yaml           ← DynamoDB Local + Admin UI
```

---

## Veelvoorkomende problemen

| Probleem | Oplossing |
|---|---|
| `Connection refused` op DynamoDB | `docker compose up -d` eerst uitvoeren |
| `401 Unauthorized` | Token verlopen of verkeerde credentials — nieuw token ophalen |
| `400 Bad Request` bij PATCH | Veld is `settable: false`, waarde buiten grenzen, of ongeldig pad |
| `404 Not Found` | Device ID bestaat niet — check met GET /admin/devices |
| Lege device lijst na opstart | Tabel bestaat maar is leeg — voeg devices toe via Admin UI of verwijder de tabel via DynamoDB Admin UI zodat de seeder opnieuw draait |
| Tests falen | Geen Docker nodig — controleer of alle packages geïnstalleerd zijn via `dotnet restore` |

---

*HvacMock Platform · Groep 8 · Toegepaste Informatica · Integration Project 2 · 2025–2026*
