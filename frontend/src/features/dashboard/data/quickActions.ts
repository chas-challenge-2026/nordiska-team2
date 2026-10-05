import transferIcon from "../../../assets/svg/transfer-dark.svg";
// import downloadIcon from "../../../assets/svg/download-dark.svg";
import bankIdIcon from "../../../assets/svg/bankid-dark.svg";
import cardIcon from "../../../assets/svg/credit-dark.svg";
import walletIcon from "../../../assets/svg/wallet-dark.svg";
import aiBuddyIcon from "../../../assets/svg/ai-buddy-dark.svg";
import type { QuickAction } from "../../../types/quickActions";

export const quickActions: QuickAction[] = [
    {
        id: "transaction",
        label: "Insättning och uttag",
        icon: bankIdIcon,
        size: "small",
    },
    {
        id: "transfer",
        label: "Överföring",
        icon: transferIcon,
        size: "small",
    },
        {
        id: "savingGoals",
        label: "Skapa sparmål",
        icon: walletIcon,
        size: "small",
    },
    //     {
    //     id: "download",
    //     label: "Ladda ner skatterapp.",
    //     icon: downloadIcon,
    //     size: "xsmall",
    // },
        {
        id: "loanApplication",
        label: "Ansök om lån",
        icon: cardIcon,
        size: "small",
    },
        {
        id: "aiBuddy",
        label: "Ai Kompis",
        icon: aiBuddyIcon,
        size: "small",
    },
                        
]