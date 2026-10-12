import transferIcon from "../../../assets/svg/transfer-dark.svg";
// import downloadIcon from "../../../assets/svg/download-dark.svg";
import bankIdIcon from "../../../assets/svg/bankid-dark.svg";
import cardIcon from "../../../assets/svg/credit-dark.svg";
import walletIcon from "../../../assets/svg/wallet-dark.svg";
import aiBuddyIcon from "../../../assets/svg/ai-buddy-dark.svg";
import type { QuickAction } from "../../../types/quickActions";

// label är nycklar i språkfilerna, de översätts med t() i QuickActions.tsx
export const quickActions: QuickAction[] = [
    {
        id: "transaction",
        label: "dashboard.quickActions.transaction",
        icon: bankIdIcon,
        size: "small",
    },
    {
        id: "transfer",
        label: "dashboard.quickActions.transfer",
        icon: transferIcon,
        size: "small",
    },
        {
        id: "savingGoals",
        label: "dashboard.quickActions.savingGoals",
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
        label: "dashboard.quickActions.loanApplication",
        icon: cardIcon,
        size: "small",
    },
        {
        id: "aiBuddy",
        label: "dashboard.quickActions.aiBuddy",
        icon: aiBuddyIcon,
        size: "small",
    },
                        
]