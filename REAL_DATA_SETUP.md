# 🔄 Configurazione Dati Reali - Interactive Brokers

## ✅ Status
Bot 3 è **configurato per usare dati reali** da Interactive Brokers API.

**Modalità Attuale**: 
- ✅ Connessione API implementata
- ✅ Fallback automatico a dati demo se API non disponibile
- ✅ Error handling robusto

---

## 🚀 Come Attivare Dati Reali

### Step 1: Verificare TWS/Gateway Attivo
```bash
# TWS deve stare acceso con API abilitato
# Settings → API → Enable Client Portal

# Verifica connessione:
curl https://localhost:5000/api/portals/fsession/validate
```

### Step 2: Ottenere Contract IDs Reali

I **Contract IDs** sono gli identificatori unici di strumenti in Interactive Brokers.

**Come trovarli:**
1. Apri TWS
2. Vai a **Market Data** tab
3. Cerca il simbolo (es: ENI.MI)
4. Leggi il **Contract ID** dalla colonna "Con ID"

**Esempio:**
```
Simbolo: ENI.MI
Contract ID: 123456789
```

### Step 3: Aggiornare la Mappa Contract IDs

Nel file `InteractiveBrokersDataService.cs`, aggiorna il metodo `GetContractId()`:

**Oggi (PLACEHOLDER):**
```csharp
private string GetContractId(string ibSymbol)
{
    return ibSymbol switch
    {
        "ENI.MI" => "272093",      // ← Placeholder
        "ISP.MI" => "272093",      // ← Placeholder
        // ... rest
    };
}
```

**Aggiorna con Contract IDs reali:**
```csharp
private string GetContractId(string ibSymbol)
{
    return ibSymbol switch
    {
        "ENI.MI" => "YOUR_REAL_ENI_CONID",      // ← Ottieni da TWS
        "ISP.MI" => "YOUR_REAL_ISP_CONID",      // ← Ottieni da TWS
        "UCG.MI" => "YOUR_REAL_UCG_CONID",      // ← Ottieni da TWS
        // ... rest
    };
}
```

---

## 📊 Dati che Riceverai

### Quote in Tempo Reale
- Prezzo attuale
- Bid/Ask spread
- Volume

### Candele Storiche
- Open, High, Low, Close
- Volume
- Timeframe: 1m, 5m, 15m, 30m, 1h, 4h, 1d

---

## 🔧 Implementazione Attuale

### Metodi Implementati

#### 1. `GetMarketPriceAsync()`
```csharp
// Chiama API IB: /iserver/marketdata/snapshot
// Fallback automatico a dati demo se fallisce
// Rate limiting: 100ms tra chiamate
```

**Flusso:**
1. ✅ Verifica sessione attiva
2. ✅ Ottiene Contract ID
3. ✅ Chiama `/iserver/marketdata/snapshot`
4. ✅ Fallback a demo se falsa

#### 2. `GetRealCandlesFromIbAsync()`
```csharp
// Chiama API IB: /iserver/marketdata/history
// Supporta intervalli: 1m, 5m, 15m, 30m, 1h, 4h, 1d
```

**Flusso:**
1. ✅ Valida Contract ID
2. ✅ Converte intervallo formato IB
3. ✅ Chiama `/iserver/marketdata/history`
4. ✅ Parse JSON response
5. ✅ Fallback a candele demo

---

## 📝 Formato API Responses

### Snapshot (Prezzo Attuale)
```json
[
  {
    "id": "272093",
    "last": "14.85",
    "bid": "14.83",
    "ask": "14.87",
    "volume": 5000000
  }
]
```

### History (Candele)
```json
{
  "bars": [
    [1694180400000, "14.80", "14.90", "14.75", "14.85", "1500000"],
    [1694184000000, "14.85", "14.95", "14.82", "14.92", "1600000"]
  ]
}
```

---

## 🐛 Troubleshooting

### ❌ "Sessione non attiva"
```
⚠️  Sessione non attiva, uso dati demo
```

**Soluzione:**
1. Verifica che TWS sia aperto
2. Settings → API → Client Portal Settings → Enable
3. Gateway deve essere in ascolto su localhost:5000
4. Riavvia Bot 3

### ❌ "Contract ID non trovato"
```
⚠️  Contract ID non trovato per ENI.MI
```

**Soluzione:**
1. Apri TWS
2. Cerca ENI.MI in Market Data
3. Copia il vero Contract ID
4. Aggiorna in `GetContractId()` metodo

### ❌ "API IB non disponibile"
```
⚠️  API IB non disponibile, uso dati demo
```

**Soluzione:**
- Verifica `curl https://localhost:5000/api/portals/fsession/validate`
- Se 401 Unauthorized: accedi a TWS
- Se timeout: controlla firewall

### ✅ "Da IB"
```
✅ ENI.MI: $14.85 (da IB)
✅ ENI.MI: 100 candele caricate da IB
```

**Significa:** Funziona perfettamente! ✨

---

## 🔄 Flusso Completo Avvio

```
dotnet run
    ↓
🔌 Connessione a Interactive Brokers...
    ↓
✅ Gateway Interactive Brokers connesso
    ↓
⏱️  Ciclo #1 - 2026-09-08 14:30:00
    ↓
📈 Analizzando 15 titoli mercato italiano...
    ↓
✅ ENI.MI: $14.85 (da IB)
✅ ENI.MI: 100 candele caricate da IB
✅ ISP.MI: $3.45 (da IB)
    ↓
📊 Analisi completata
    ↓
✉️  Email inviata (se segnale trovato)
```

---

## 📚 Documentazione Ufficiale IB

- [Client Portal API Docs](https://www.interactivebrokers.com/en/software/clientportalapi/)
- [Market Data Endpoints](https://www.interactivebrokers.com/en/software/clientportalapi/marketdata.html)
- [Contract Search](https://www.interactivebrokers.com/en/software/clientportalapi/contractsearch.html)

---

## ✨ Prossimi Miglioramenti

- [ ] Caching dei Contract IDs
- [ ] Supporto per più timeframe simultanei
- [ ] Streaming dati in tempo reale (WebSocket)
- [ ] Gestione errori API avanzata
- [ ] Metriche di uptime API

---

**Bot Status**: 🟢 Pronto per dati reali  
**Ultimo aggiornamento**: 2026-09-08  
**Modalità**: Hybrid (reale + fallback demo)
