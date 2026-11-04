# Frontend: teststatus

**Datum:** 2026-11-04
**Resultat:** 12 testfiler, 45 tester, alla går igenom.
**Verktyg:** Vitest + React Testing Library (jsdom)
**Kör testerna:** `npm test` i mappen `frontend`

## Sammanfattning

Vi har prioriterat det som är mest riskfyllt: fönstren där kunden flyttar pengar. Testerna kontrollerar att felaktiga belopp, otillräckligt saldo och överföring till samma konto stoppas innan något skickas till backend, och att giltiga transaktioner skickas till rätt adress med rätt data. Vi testar också kontoutdrag, notiser, felmeddelanden och våra byggklossar.

Frontendtesterna pratar inte med den riktiga backenden. Den byts ut mot låtsasversioner (mockar), så att testerna är snabba och bara testar frontendens egen logik. Att backend räknar rätt testas i backendens egna tester mot en riktig databas.

## Vad som testas

| Område | Testfil | Antal | Vad testerna kontrollerar |
|---|---|---|---|
| Insättning och uttag | `TransactionModal.test.tsx` | 4 | Uttag större än saldot och belopp 0 stoppas. Giltig insättning och giltigt uttag skickas till rätt endpoint med rätt data. |
| Överföring | `TransferModal.test.tsx` | 3 | Otillräckligt saldo och samma från- och tillkonto stoppas. En giltig överföring skickas med båda kontona. |
| Sparmål | `CreateSavingsGoalsModal.test.tsx` | 3 | Målbelopp 0 eller tomt stoppas. Ett giltigt mål skickas. Det första testet är ett regressionstest efter en bugg. |
| Kontoutdrag | `BankStatementsCard.test.tsx` | 5 | De senaste 24 månaderna visas. Rätt konto, år och månad skickas (1–12, inte 0–11). Knapparna låses vid nedladdning. Vänligt meddelande vid 404 och vid andra fel. |
| Notiser | `NotificationsCard.test.tsx` | 5 | Laddning, tomt läge och fel visas. Notiserna översätts vid språkbyte. En notis visas när en väntande notis blir skickad. |
| Felmeddelanden | `apiErrorMessage.test.tsx` | 7 | Fel från backend blir begripliga meddelanden: kända felkoder, okända koder, valideringsfel, för många försök (429), nätverksfel, och att funktionen aldrig kraschar. |
| BankID-PIN | `DemoBankIdPinModal.test.tsx` | 4 | Bara siffror tas emot. Knappen är låst tills sex siffror är ifyllda. Rätt funktion anropas vid bekräfta och avbryt. |
| FAQ | `FaqQuestionCard.test.tsx` | 3 | Frågan visas, svaret är dolt från början och visas efter klick. |
| Kontokort | `AccountCard.test.tsx` | 2 | Namn, kontonummer, saldo och ränta visas. Räntaraden försvinner om räntan saknas. |
| Byggklossar | `Alert.test.tsx`, `ListItem.test.tsx`, `Footer.test.tsx` | 9 | Rätt text, färg och synlighet. Valfritt innehåll visas bara när det skickas in. |

## Hur vi testar

- **Som en användare.** Testerna hittar knappar och fält på den text användaren ser, t.ex. `getByRole("button", { name: "Skicka" })`. De håller därför även om koden skrivs om.
- **Med riktig språkfil.** Testerna letar efter den svenska texten, så de kontrollerar också att översättningarna finns.
- **Med mockad backend.** `apiClient`, `useAccounts` och `useAlert` byts ut. Då kan vi kontrollera exakt vad som *skulle* ha skickats, utan att en server behöver köras.

## Vad som inte testas än

- Hela sidor, t.ex. Dashboard och Kontohantering.
- Hooks, t.ex. `useAccounts` och `useTransactions`.
- Uträkningen i stapeldiagrammet (`buildSeries`).
- Token-förnyelsen i `client.ts` och inloggningsflödet.
- End-to-end-tester i en riktig webbläsare mot riktig backend.
- Testtäckning (coverage) mäts inte.

## Nästa steg

1. Testa `buildSeries` i stapeldiagrammet. Det är en ren funktion och lätt att testa.
2. Testa inloggningen och token-förnyelsen.
3. Lägg till några end-to-end-tester med Playwright för de viktigaste flödena: logga in, sätt in pengar, för över mellan konton och skapa ett sparmål.
4. Kör frontendtesterna automatiskt i GitHub Actions vid varje push.
