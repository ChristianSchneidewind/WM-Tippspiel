# Scraper API Dokumentation

## Übersicht
Die Scraper API ermöglicht es, Spielergebnisse automatisch in das TippSpiel-System einzutragen. Dies ist ideal für:
- ⚽ Live-Scraper von Fußball-Websites
- 🤖 Automatische Ergebnis-Updates
- 📱 Integration mit externen Quellen
- 🔄 Batch-Updates

## Sicherheit
Alle API-Endpoints benötigen ein gültiges **API-Token** im Header:
```
X-API-Token: your-secret-api-token-here
```

**⚠️ Wichtig:** Token in `appsettings.json` ändern (nicht im Repo committen!)

## Endpoints

### 1. Einzelnes Ergebnis eintragen
```
POST /api/scraper/update-result
Content-Type: application/json
X-API-Token: your-secret-api-token-here

{
  "gameId": 1,
  "homeScore": 2,
  "awayScore": 1
}
```

**Response (Erfolg):**
```json
{
  "success": true,
  "message": "Ergebnis aktualisiert: Deutschland 2:1 Frankreich",
  "gameId": 1,
  "homeScore": 2,
  "awayScore": 1
}
```

**Response (Fehler):**
```json
{
  "error": "Ungültige GameId"
}
```

---

### 2. Mehrere Ergebnisse auf einmal eintragen (Batch)
```
POST /api/scraper/update-results-batch
Content-Type: application/json
X-API-Token: your-secret-api-token-here

[
  {
	"gameId": 1,
	"homeScore": 2,
	"awayScore": 1
  },
  {
	"gameId": 2,
	"homeScore": 0,
	"awayScore": 3
  },
  {
	"gameId": 3,
	"homeScore": 1,
	"awayScore": 1
  }
]
```

**Response:**
```json
{
  "success": true,
  "message": "3 Ergebnisse verarbeitet",
  "results": [
	{
	  "gameId": 1,
	  "success": true
	},
	{
	  "gameId": 2,
	  "success": true
	},
	{
	  "gameId": 3,
	  "success": true
	}
  ]
}
```

---

### 3. Health-Check
```
GET /api/scraper/health
```

**Response:**
```json
{
  "status": "ok",
  "timestamp": "2026-05-20T15:30:45.123Z"
}
```

---

## Beispiele mit cURL

### Einzelnes Ergebnis:
```bash
curl -X POST http://localhost:5000/api/scraper/update-result \
  -H "Content-Type: application/json" \
  -H "X-API-Token: your-secret-api-token-here" \
  -d '{
	"gameId": 1,
	"homeScore": 2,
	"awayScore": 1
  }'
```

### Batch-Update:
```bash
curl -X POST http://localhost:5000/api/scraper/update-results-batch \
  -H "Content-Type: application/json" \
  -H "X-API-Token: your-secret-api-token-here" \
  -d '[
	{"gameId": 1, "homeScore": 2, "awayScore": 1},
	{"gameId": 2, "homeScore": 0, "awayScore": 3}
  ]'
```

### Health-Check:
```bash
curl http://localhost:5000/api/scraper/health
```

---

## Beispiele mit Python

```python
import requests
import json

API_URL = "http://localhost:5000/api/scraper"
API_TOKEN = "your-secret-api-token-here"

headers = {
	"X-API-Token": API_TOKEN,
	"Content-Type": "application/json"
}

# Einzelnes Ergebnis
result_data = {
	"gameId": 1,
	"homeScore": 2,
	"awayScore": 1
}

response = requests.post(
	f"{API_URL}/update-result",
	json=result_data,
	headers=headers
)

print(response.json())
```

---

## Was passiert automatisch nach einem Update?

Wenn ein Ergebnis eingetragen wird:

1. ✅ **Ergebnis wird in DB gespeichert**
2. ✅ **Alle Tipps für dieses Spiel werden neu berechnet**
   - 3 Punkte für korrektes Ergebnis
   - 2 Punkte für richtige Tendenz
3. ✅ **SignalR sendet Live-Updates an alle Clients**
   - `ReceiveResultUpdate` - Spielergebnis
   - `ReceiveGroupStandingsUpdate` - Gruppentabelle
   - `ReceiveRankingsUpdate` - Rankings
4. ✅ **Browser aktualisieren sich automatisch** ohne Neuladen

---

## Fehlerbehandlung

| HTTP Code | Bedeutung | Beispiel |
|-----------|-----------|---------|
| 200 | Erfolg | Ergebnis eingetragen |
| 400 | Ungültige Daten | Tore < 0 oder GameId invalid |
| 401 | Authentifizierung fehlgeschlagen | Token fehlt oder ungültig |
| 404 | Spiel nicht gefunden | GameId existiert nicht |
| 500 | Server-Fehler | Unerwarteter Fehler |

---

## Best Practices

✅ **Empfohlen:**
- Token sicher speichern (Umgebungsvariablen)
- Batch-Updates verwenden (effizienter)
- Health-Check vor dem Scraping
- Retry-Logik für fehlerhafte Requests
- Logging implementieren

❌ **Nicht empfohlen:**
- Token im Code hardcodieren
- Token im GitHub committen
- Zu häufige Updates (Ratelimit?)
- Ungültige GameIds verwenden

---

## Konfiguration in appsettings.json

```json
"Scraper": {
  "ApiToken": "ChangeThisInProduction!"
}
```

**Für Production:**
```bash
# Setze über Umgebungsvariable:
export Scraper__ApiToken="super-sicheres-token-hier"
```

---

## Support

Bei Fragen oder Problemen: Schau in `Controllers/ScraperController.cs`
