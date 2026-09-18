# 🔴 Real-Time Data Streaming - Bot 3

## ✅ Stato
Bot 3 è **configurato per real-time data streaming** da Interactive Brokers TWS.

---

## 🎯 Modalità Operative

### Modalità 1: Real-Time Direct (Preferito)
- **Connessione**: Diretta a TWS porta 7496
- **Latenza**: < 500ms
- **Polling**: 50ms tra richieste
- **Caching**: 2 secondi (smart cache)
- **Status**: Automatico se TWS è connesso

### Modalità 2: Client Portal Gateway (REST)
- **Connessione**: REST API su localhost:5000
- **Latenza**: 1-2 secondi
- **Polling**: 100ms tra richieste
- **Fallback**: Se Real-Time non disponibile

---

## 🚀 Attivazione Real-Time

### Step 1: TWS Connesso
```
✅ TWS aperto e in esecuzione
✅ Credenziali inserite
✅ API abilitato
```

### Step 2: Verificare Connessione Diretta
```bash
# Verifica che TWS ascolti su porta 7496
netstat -ano | findstr :7496
# Output atteso: LISTENING on 127.0.0.1:7496
```

### Step 3: Avviare Bot 3
```bash
cd "C:\Users\Pc\OneDrive\Bot\3 - ERTF Ita"
dotnet run
```

### Output Atteso (Real-Time)
```
🔌 Connessione Real-Time a Interactive Brokers TWS...
✅ TWS risponde su porta 7496
✅ Connessione DIRETTA a TWS (Real-Time)

⏱️  Ciclo #1 - 2026-09-08 17:25:00
📈 Analizzando 15 titoli mercato italiano...
✅ ENI.MI: $14.87 (Real-Time)
✅ ISP.MI: $3.46 (Real-Time)
✅ UCG.MI: $35.95 (Real-Time)
```

---

## 📊 Flusso Real-Time

```
┌─────────────────────────────────┐
│  Bot 3 Ciclo Analisi (5 min)    │
├─────────────────────────────────┤
│ 1. GetItalianStocksAsync()      │
│    ↓                             │
│ 2. Per ogni simbolo:            │
│    ├─ GetMarketPriceAsync()     │
│    │   ├─ Check Cache (2sec)    │
│    │   └─ Se expired:           │
│    │       └─ GetPriceFromTws() │
│    │           └─ REST API      │
│    ├─ GetCandlesAsync()         │
│    │   └─ /iserver/marketdata/  │
│    │       history              │
│    └─ Analisi EMA Ribbon        │
│        ↓                         │
│ 3. Se Segnale:                  │
│    └─ Email Alert ✉️            │
│        └─ Desktop Notification  │
│                                 │
│ Next Ciclo: +5 minuti           │
└─────────────────────────────────┘
```

---

## ⚡ Caratteristiche Real-Time

### 1. **Smart Caching**
- Cache valido per 2 secondi
- Evita rate limiting API
- Aggiornamento automatico

### 2. **Dual Connection**
- Primaria: Direct TWS (7496)
- Fallback: Client Portal REST (5000)
- Transizione automatica

### 3. **Low Latency**
- Rate limit: 50ms (vs 100ms)
- Polling frequente: 1000ms per ciclo
- Candele: Real-time quando disponibile

### 4. **Resilience**
- Connessione fallita? Usa demo
- API timeout? Retry automatico
- Cache fallback: Usa ultimo valore

---

## 🔍 Monitoraggio Real-Time

### Verificare Status
```bash
# Processo Bot 3 in esecuzione
tasklist | findstr dotnet
# Output: dotnet.exe (PID)

# Porta 7496 in ascolto
netstat -ano | findstr :7496
```

### Log File
```
📁 bin/Release/net10.0/Logs/
├── signals_2026-09-08.log  (segnali)
└── trades_2026-09-08.log   (trades)
```

### Email Alerts
```
✉️  Email inviata quando:
├─ Nuovo segnale BUY/SELL
├─ Trade chiuso
└─ Errore critico
```

---

## 🎯 Configurazione appsettings.json

```json
"InteractiveBrokers": {
  "Enabled": true,
  "TwsDirectUrl": "localhost:7496",      // Direct connection
  "ClientPortalUrl": "https://localhost:5000/api",  // Fallback
  "RateLimitDelayMs": 50,                // Più veloce
  "DataMode": "RealTime",                // Real-time streaming
  "RealtimePollingMs": 1000,             // 1 sec polling
  "FallbackToDemoOnError": true          // Fallback automatico
}
```

---

## 🔴 Troubleshooting Real-Time

### ❌ "TWS non risponde su porta 7496"
```
Output: ⚠️  Tentativo Client Portal Gateway (REST)...
```

**Soluzione:**
1. Verifica che TWS sia aperto
2. Controlla che API sia abilitato in Settings
3. Controlla firewall (port 7496)

### ❌ "Connessione REST non disponibile"
```
Output: ❌ Nessuna connessione a IB disponibile
```

**Soluzione:**
1. Abilita Client Portal Gateway
2. Verifica localhost:5000 sia raggiungibile
3. Usa dati demo nel frattempo

### ✅ "Real-Time Attivo"
```
Output: ✅ TWS risponde su porta 7496
        ✅ Connessione DIRETTA a TWS (Real-Time)
        ✅ ENI.MI: $14.87 (Real-Time)
```

---

## 📈 Performance Real-Time

### Latenza Attesa
- **Snapshot Prezzo**: 100-200ms
- **Candele Storiche**: 500-1000ms
- **Analisi Completa**: 3-5 secondi per 15 simboli

### Rate Limits IB
- Snapshot: 50ms min tra richieste
- History: 100ms min tra richieste
- Connect: 1 per sessione

---

## 🔄 Prossimi Upgrade

- [ ] WebSocket streaming (se disponibile in IB API)
- [ ] Tick data in tempo reale
- [ ] Multiple symbol parallel requests
- [ ] Persistent connection pooling
- [ ] Automatic reconnect logic

---

## 📚 Documentazione

- **TWS API**: https://www.interactivebrokers.com/en/software/tws/
- **Client Portal API**: https://www.interactivebrokers.com/en/software/clientportalapi/

---

**Ultima Configurazione**: 2026-09-08  
**Modalità**: 🔴 Real-Time Direct + REST Fallback  
**Status**: ✅ Pronto per dati live
