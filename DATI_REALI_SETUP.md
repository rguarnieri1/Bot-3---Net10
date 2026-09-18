# Setup Dati Reali - Bot 3 ERTF Ita

## 🚀 Attivazione Dati Reali da Finnhub

Il Bot 3 è ora configurato per recuperare dati reali in tempo reale dal mercato italiano tramite **Finnhub**.

### 📋 Prerequisiti

1. **API Key Finnhub Gratuita** (necessaria)
   - Visita: https://finnhub.io
   - Registrati (gratuito)
   - Copia la tua API Key

### ⚙️ Configurazione

#### Step 1: Inserisci la API Key

Modifica il file `appsettings.json` nella cartella del Bot 3:

```json
"Finnhub": {
  "Enabled": true,
  "ApiUrl": "https://finnhub.io/api/v1",
  "ApiKey": "YOUR_API_KEY_QUI",  // ← Sostituisci con la tua API Key
  "UseForRealData": true,
  "RateLimitPerMinute": 60
}
```

#### Step 2: Aggiorna il file InteractiveBrokersDataService.cs

Sostituisci:
```csharp
private const string FinnhubApiKey = "YOUR_FINNHUB_API_KEY";
```

Con la tua API Key:
```csharp
private const string FinnhubApiKey = "c1234567890abcdef";
```

#### Step 3: Riavvia il Bot

```bash
cd "C:\Users\Pc\OneDrive\Bot\3 - ERTF Ita"
dotnet run --configuration Release
```

### 📊 Dati Recuperati

Una volta configurato, il bot recupererà dati reali in tempo reale per:

- **ENI** - Eni SpA
- **ISP** - Intesa Sanpaolo
- **UCG** - UniCredit
- **STM** - STMicroelectronics
- **ENEL** - Enel SpA
- E altri 10 titoli del mercato italiano

### 🔄 Rate Limit

- **Piano Gratuito**: 60 chiamate al minuto
- **Intervallo Bot**: 15 minuti
- **Calcolo**: ~1 chiamata ogni 5-10 secondi (ben entro il limite)

### ✅ Verifica

Nel log del bot vedrai:
```
✅ ENI.MI: €14.85 (Finnhub Real-Time)
✅ ISP.MI: €3.45 (Finnhub Real-Time)
```

Invece di:
```
⚠️  ENI.MI: Usando prezzo demo (dati reali non disponibili)
```

### 🔗 Link Utili

- Finnhub: https://finnhub.io
- Documentazione: https://finnhub.io/docs/api
- Dashboard: https://finnhub.io/dashboard

---

**Data**: 2026-09-09
**Bot**: 3 - ERTF Ita
**Status**: Pronto per dati reali
