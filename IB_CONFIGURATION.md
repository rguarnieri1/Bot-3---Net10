# Configurazione Interactive Brokers - Bot 3 ERTF Ita

## 📋 Riepilogo
Bot 3 è stato configurato per scaricare i dati di mercato dal **Mercato Italiano** tramite **Interactive Brokers** (IB).

### Cambio da Crypto.com a Interactive Brokers
- ❌ **Vecchio**: `CryptoDataService` (scaricava criptovalute da Crypto.com/Bybit)
- ✅ **Nuovo**: `InteractiveBrokersDataService` (scarica dati mercato italiano da IB)

---

## 🔧 Setup Iniziale

### Prerequisiti
1. **Account Interactive Brokers** attivo (roberto.guarnieri2@gmail.com)
2. **TWS (Trader Workstation)** installato sul PC
3. **Client Portal Gateway** attivato in TWS

### Step 1: Attivare Client Portal Gateway in TWS

1. Apri **Interactive Broker's TWS**
2. Vai a **Settings** → **API** → **Settings**
3. Seleziona **Enable ActiveX and Socket Clients**
4. Vai a **Settings** → **API** → **Client Portal Settings**
5. Clicca **Enable** su Client Portal
6. Il Gateway si avvierà automaticamente su `https://localhost:5000`

### Step 2: Configurare le Credenziali (Opzionale)

Nel file `Program.cs`, se vuoi fornire username/password:

```csharp
var connected = await _dataService.ConnectAsync("username", "password");
```

Altrimenti lascia vuoto (il sistema tenterà la connessione al Gateway attivo):

```csharp
var connected = await _dataService.ConnectAsync("", "");
```

---

## 📊 Simboli Configurati (Mercato Italiano)

Il bot monitora questi titoli della Borsa Italiana:

| Simbolo | Nome | Settore |
|---------|------|---------|
| ENI | Eni SpA | Energia |
| ISP | Intesa Sanpaolo | Finanza |
| UCG | UniCredit | Finanza |
| TIT | Telecom Italia | Telecomunicazioni |
| STM | STMicroelectronics | Tecnologia |
| ENEL | Enel SpA | Energia |
| AZM | Mediobanca | Finanza |
| FTSEMIB | FTSE MIB Index | Indice |
| E altri... | | |

### Aggiungere/Modificare Simboli

Nel codice `InteractiveBrokersDataService.cs`, modifica la lista:

```csharp
private readonly List<string> _italianMarketSymbols = new()
{
    "ENI", "ISP", "UCG", // ... aggiungi i tuoi
};
```

Oppure a runtime:
```csharp
_dataService.AddSymbol("IBE"); // Iberdrola
```

---

## 🔌 Connessione a Interactive Brokers

### Metodo 1: Client Portal Gateway (CONSIGLIATO)
- ✅ Più semplice
- ✅ Non richiede Java
- ✅ Supporto ufficiale IB
- ⚠️ Deve stare acceso come servizio separato

**Status attuale**: Implementato nel codice

### Metodo 2: TWS API (Alternativa)
- ✅ Più robusto
- ⚠️ Richiede Java
- ⚠️ Più complesso da configurare

---

## 🚀 Avviare il Bot

```bash
cd "C:\Users\Pc\OneDrive\Bot\3 - ERTF Ita"
dotnet run
```

Output atteso:
```
🤖 Bot Mercato Italiano - ERTF Ita avviato!
📊 Fonte Dati: Interactive Brokers (Mercato Italiano)
📊 Monitoraggio ogni 5 minuti...

🔌 Tentativo connessione Interactive Brokers...
✅ Connesso a Interactive Brokers!

⏱️ Ciclo #1 - 2026-09-08 14:30:00
📈 Analizzando 15 titoli mercato italiano...
```

---

## 📈 Strategie Abilitate

Il bot usa le **stesse strategie** di Bot 1 e Bot 2:

1. **EMA Ribbon Trend Following** (Principale)
   - EMAs: 5, 10, 20, 50
   - Win Rate atteso: 60-62%

2. **Bullish Divergence V2** (Backup)
   - Attivata se EMA non genera segnali

---

## ⚠️ Troubleshooting

### Errore: "Client Portal Gateway non risponde"
```
❌ Client Portal Gateway non risponde su localhost:5000
```

**Soluzione**:
1. Apri TWS
2. Vai a **Settings** → **API** → **Client Portal Settings**
3. Clicca **Enable** (dovrebbe avviarsi automaticamente)
4. Riavvia il bot

### Dati demo se IB non disponibile
Se la connessione non riesce, il bot continua automaticamente con **dati demo** per il testing.

---

## 📊 Modalità Trading

**🚀 MODALITA' ATTUALE: TRADING REALE ATTIVATO**

**Configurazione Attiva**:
- ✅ Connessione: Interactive Brokers Account Reale
- ✅ Account Type: REAL
- ✅ Trading Mode: LIVE
- ✅ Quote: Tempo reale da Interactive Brokers
- ✅ Candele storiche: 1m, 5m, 1h, 4h, 1d
- ✅ Ordini: Diretti via API su account reale
- ✅ Fallback Demo: DISABILITATO (nessun fallback)

---

## ✅ Attivazione Trading Reale

**Data Attivazione**: 2026-09-09  
**Abilitato da**: Utente (roberto.guarnieri2@gmail.com)

### Checklist Pre-Avvio
- [x] Interactive Brokers Account Configurato
- [x] TWS (Trader Workstation) Installato  
- [x] Client Portal Gateway Abilitato in TWS
- [x] API Abilitata: Settings → API → Settings
- [x] appsettings.json Configurato per modalità LIVE
- [x] Account Type: REAL (non demo)
- [x] Fallback Demo: DISABILITATO

---

## 📞 Riferimenti

- **Interactive Brokers API Docs**: https://www.interactivebrokers.com/en/software/api/
- **Client Portal API**: https://www.interactivebrokers.com/en/software/clientportalapi/
- **Simboli Borsa Italiana**: Usa formato `.MI` (es: `ENI.MI`)

---

**Ultimo aggiornamento**: 2026-09-09  
**Bot**: 3 - ERTF Ita  
**Stato**: 🚀 TRADING REALE ATTIVATO
