# Configurazione Fallback System - Bot 3 ERTF Ita

## 📊 Sistema di Fallback Implementato

Il Bot 3 ora utilizza un sistema di fallback robusto a **2 livelli** per garantire continuità operativa:

### Gerarchia delle Fonti Dati

```
┌─────────────────────────────────────────────────────┐
│ PRIMARIA: Interactive Brokers TWS (Porta 7497)      │
│ ✅ Real-Time | ✅ Mercato Italiano | ✅ Affidabile  │
└──────────────────┬──────────────────────────────────┘
                   │
                   ↓ (Se TWS non disponibile)
┌─────────────────────────────────────────────────────┐
│ FALLBACK 1: Finnhub API                              │
│ ✅ Real-Time | ✅ Dati Globali | ✅ Robusto         │
└──────────────────┬──────────────────────────────────┘
                   │
                   ↓ (Se Finnhub non disponibile)
┌─────────────────────────────────────────────────────┐
│ ULTIMO RICORSO: Demo Data                            │
│ ⚠️  Simulato | ⚠️  Solo Testing | ⚠️  Non Live    │
└─────────────────────────────────────────────────────┘
```

---

## 🔧 Configurazione nel config.json

```json
{
  "fallbackSystem": {
    "enabled": true,
    "primary": "InteractiveBrokersTWS",
    "fallback": [
      {
        "order": 1,
        "source": "FinnhubAPI",
        "endpoint": "https://finnhub.io/api/v1",
        "enabled": true
      }
    ]
  }
}
```

### Parametri

- **enabled**: Abilita/disabilita il sistema di fallback
- **primary**: Fonte dati principale (**InteractiveBrokersTWS** - sempre primaria)
- **fallback**: Array di fallback ordinati per priorità
  - **order**: Priorità di esecuzione (1 = primo fallback dopo TWS)
  - **source**: Nome della fonte (FinnhubAPI)
  - **endpoint**: URL API
  - **enabled**: Abilita/disabilita questo fallback

### Flag di Configurazione nel Codice

**In `InteractiveBrokersDataService.cs`:**

```csharp
private bool _useFinnhubForRealData = false; // ← DISABILITATO
// Finnhub è SOLO fallback, non source primaria
```

**Implicazioni:**
- ✅ TWS è SEMPRE tentato per primo
- ✅ Finnhub è utilizzato SOLO se TWS fallisce
- ✅ Demo data è l'ultima risorsa

---

## 🔐 API Key Finnhub

La chiave API è già configurata nel codice:

```csharp
private const string FinnhubApiKey = "daggpk9r01quf8mu8vkgdaggpk9r01quf8mu8vl0";
```

### Cambiare la Chiave API

Se necessario cambiare la chiave, modifica il file:
**`Services/InteractiveBrokersDataService.cs` (riga 13)**

```csharp
private const string FinnhubApiKey = "YOUR_NEW_KEY_HERE";
```

---

## 🚀 Come Funziona il Fallback

### 1️⃣ Avvio del Bot

Quando il bot si avvia:

```
🔌 Tentativo connessione Interactive Brokers TWS Socket (porta 7497)...
✅ Connessione DIRETTA a TWS Socket ATTIVA (Real-Time)
```

Oppure se TWS non è disponibile:

```
❌ TWS non disponibile sulla porta 7497
🔄 Attivazione FALLBACK SYSTEM → Tentando connessione Finnhub...
✅ Fallback Finnhub ATTIVO (Real-Time Market Data)
```

### 2️⃣ Durante il Monitoraggio

Quando il bot scarica i prezzi:

**Scenario 1 - Con TWS attivo (Primaria):**
```
✅ ENI: €14.85 (IB TWS - Primary)
✅ ISP: €3.45 (IB TWS - Primary)
✅ UCG: €35.92 (IB TWS - Primary)
```

**Scenario 2 - TWS offline, Finnhub attivo (Fallback):**
```
⚠️  ENI: TWS non ha dati, tentando Finnhub...
✅ ENI: €14.85 (Finnhub - Fallback)
✅ ISP: €3.45 (Finnhub - Fallback)
✅ UCG: €35.92 (Finnhub - Fallback)
```

**Scenario 3 - Sia TWS che Finnhub offline (Demo):**
```
⚠️  ENI: TWS non ha dati, tentando Finnhub...
⚠️  ENI: Finnhub non ha dati, usando demo...
✅ ENI: €14.85 (Demo Data)
```

---

## 📊 Verifica dello Stato

Il bot stampa lo stato della fonte dati all'avvio:

```csharp
// Nel codice - accedi con:
var status = _dataService.GetDataSourceStatus();
// Output: "📊 Data Source: Finnhub API (Fallback) [FALLBACK ATTIVO]"
```

---

## ⚙️ Configurazione Consigliata

### Per Produzione (Trading Reale)

```json
{
  "fallbackSystem": {
    "enabled": true,
    "primary": "InteractiveBrokersTWS",
    "fallback": [
      {
        "order": 1,
        "source": "FinnhubAPI",
        "enabled": true
      }
    ]
  }
}
```

**Comportamento:**
- Tenta SEMPRE IB TWS per primo
- Fallback a Finnhub se TWS fallisce
- Nessun downtime operativo

### Per Testing/Backtest

```json
{
  "fallbackSystem": {
    "enabled": false
  }
}
```

**Comportamento:**
- Usa solo dati demo
- Utile per testing offline

---

## 🔍 Troubleshooting

### Problema: Bot passa a Finnhub anche se TWS è online

**Soluzione**: Verifica che TWS sia effettivamente connesso:

1. Apri TWS
2. Vai a **Settings** → **API** → **Settings**
3. Assicurati che **"Enable ActiveX and Socket Clients"** sia ✅
4. Riavvia il bot

### Problema: Finnhub ritorna "Ticker not found"

**Causa**: Il simbolo non esiste su Finnhub

**Soluzione**: Aggiungi una mappatura nel codice:

```csharp
// In InteractiveBrokersDataService.cs, metodo GetPriceFromFinnhub
string[] tickerFormats = new[]
{
    $"{ticker}.MI",      // Borsa Italiana
    ticker,              // Simbolo base
    // Aggiungi altri formati se necessario
};
```

### Problema: API Rate Limit Finnhub

**Causa**: Troppe richieste all'API Finnhub

**Soluzione**: Aumenta il `RateLimitDelayMs`:

```csharp
private const int RateLimitDelayMs = 100; // Aumentato da 50
```

---

## 📈 Performance vs Affidabilità

| Fonte | Latenza | Affidabilità | Costo | Note |
|-------|---------|--------------|-------|------|
| **IB TWS** | <100ms | 99.9% | ✅ Incluso | Principale - Altissima qualità |
| **Finnhub** | 50-200ms | 99% | 🆓 Freemium | Fallback - Buona qualità |
| **Demo** | <1ms | N/A | N/A | Solo testing |

---

## 🔔 Monitoraggio del Fallback

Il bot logga automaticamente quando attiva il fallback:

**Log File**: `bin\Release\net10.0\Logs\`

Cerca questi messaggi per verificare l'uso del fallback:

```
🔄 Attivazione FALLBACK SYSTEM
✅ Fallback Finnhub ATTIVO
⚠️  NOTA: Operando in modalità FALLBACK
```

---

## ✅ Checklist Configurazione

- [x] Fallback System abilitato nel `config.json`
- [x] Finnhub API Key configurata
- [x] TWS Desktop connessione configurata
- [x] Metodo di fallback implementato
- [x] Logging configurato
- [x] Test di fallback eseguito

---

**Ultimo Aggiornamento**: 2026-09-10  
**Bot**: 3 - ERTF Ita  
**Status**: ✅ Fallback Finnhub Operativo
