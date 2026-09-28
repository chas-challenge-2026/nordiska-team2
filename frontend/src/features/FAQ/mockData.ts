// TODO(API): Denna fil ska tas bort helt när #82 (GET /api/faq/search) är klar.
// Ersätt importer av mockCategories/mockFaqEntries med riktiga anrop.

import type { FaqEntry, FaqCategory } from "../../types/faq";

export const mockCategories: FaqCategory[] = [
  { id: "account", label: "Konto & inlogg", icon: "credit-card", questionCount: 9 },
  { id: "transactions", label: "Insättning & uttag", icon: "arrows", questionCount: 12 },
  { id: "interest", label: "Ränta", icon: "trending-up", questionCount: 7 },
  { id: "tax", label: "Skatt & rapporter", icon: "file", questionCount: 6 },
];

export const mockFaqEntries: FaqEntry[] = [
  {
    id: "1",
    question: "Hur mycket får jag låna till min bostad?",
    answer:
      "Du kan låna upp till 90 procent av bostadens värde vid köp av ny bostad. Resterande 10 procent behöver du lägga i kontantinsats",
    category: "transactions",
    keywords: ["lån", "pengar", "bostad", "ränta"],
  },
  {
    id: "2",
    question: "Aktuella räntor - vad kan jag få för ränta just nu?",
    answer: "Aktuella sparräntor för privatkunder, aktuella sparräntor för företagskunder",
    category: "interest",
    keywords: ["ränta", "utbetalning", "kvartal"],
  },
  {
    id: "3",
    question: "Sätta in eller ta ut pengar?",
    answer: "Våra bankkontor är kontantfria. Vill du sätta in pengar kan du göra det i en Bankomat. Gå till Inställningar → Konton och uppdatera ditt registrerade utbetalningskonto.",
    category: "account",
    keywords: ["uttag", "insättning", "konto"],
  },
  {
    id: "4",
    question: "Glömt min pinkod till mitt bankkort - var kan jag se den?",
    answer: "Om du har glömt din pinkod till ditt kort behöver du ersätta ditt befintliga kort och välja en egenvald pinkod via internetbanken.",
    category: "account",
    keywords: ["konto", "skydd", "kort"],
  },
  {
    id: "5",
    question: "Var hittar jag min skatterapport?",
    answer: "Under Skatterapporter kan du generera och ladda ner ditt ränteunderlag för valt år.",
    category: "tax",
    keywords: ["skatterapport", "skatt", "ränteunderlag"],
  },
]; 