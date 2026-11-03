# Frontend: beslutslogg

Vårt mål med frontend var tydligt från början: vi ville få till ett kontoflöde där pengarna faktiskt rör sig, och det skulle fungera felfritt innan vi byggde vidare. Därför tog vi en del i taget och gick inte vidare förrän den satt.

## Så här prioriterade vi

1. **Dashboard.** Vi började här eftersom det är kundens startsida och navet i hela appen. När dashboarden fanns hade vi en plats att koppla allt annat till.
2. **Inloggning.** Utan inloggning finns ingen riktig data att visa. Vi byggde e-post och lösenord först, och sedan en BankID-mock som följer samma flöde som riktigt BankID.
3. **FAQ.** En fristående del som vi kunde bygga parallellt, och som hjälper kunden innan hen behöver ringa.
4. **Insättning, uttag, överföring och historik.** Här började pengarna röra sig. Det här var det viktigaste att få rätt, så vi la mest tid på validering och på att allt uppdateras direkt.
5. **Skatterapporter.** Kunden kan ladda ner sitt skatteunderlag som PDF, skapat av vår egen PDF-motor.
6. **Kontohantering.** Öppna och avsluta konton, och en sida med allt om ett konto.
7. **Sparmål.** Det sista i kärnflödet. Det bygger på att konton och saldon redan fungerar, eftersom förloppet räknas från kontots saldo.

## Efter feedforward

När kärnflödet satt fick vi feedforward. Då byggde vi vidare med det som gör appen mer komplett:

- **Notifikationer**, så att kunden får besked när något händer på kontot.
- **Auditlog**, där kunden kan se sin egen aktivitet i appen.
- **Kontoutdrag**, som kan laddas ner per månad som PDF.
- **AI-assistent**, en smartare hjälp än FAQ:n.

Vi valde medvetet att vänta med de här delarna tills grunden fungerade. Alla bygger på att konton, transaktioner och inloggning redan sitter. Hade vi börjat med dem hade vi byggt på något som inte var stabilt än.

## Beslut vi tog längs vägen

- **React Query för all serverdata.** Frontend räknar aldrig ut saldon själv. När något ändras talar vi bara om vilken data som är gammal, och då hämtas den på nytt. Därför uppdateras allt överallt direkt.
- **Inloggningen ligger bara i minnet.** Det är säkrare än localStorage. Den förnyas tyst i bakgrunden, så kunden märker ingenting.
- **Fönster istället för nya sidor.** Insättning, överföring och sparmål öppnas i ett fönster på dashboarden, så att kunden inte tappar sin överblick.
- **Ett designsystem med Tailwind.** Alla färger och storlekar finns på ett ställe. Därför kunde vi bygga mörkt läge med en enda klass, och allt ser enhetligt ut.
- **Två språk från start.** Svenska och engelska, även felmeddelanden. Det hade varit mycket svårare att lägga till i efterhand.
- **Backend har sista ordet.** Frontend kontrollerar belopp och saldo för snabb återkoppling, men vi skickar aldrig kund-id, ränta eller kontonummer. Det bestämmer backend.
- **Vi testade det som rör pengar först.** Våra tester fokuserar på fönstren för insättning, uttag, överföring och sparmål.

Hellre ett flöde som fungerar hela vägen först, och sedan bygga ut, än många funktioner som bara nästan fungerar.
