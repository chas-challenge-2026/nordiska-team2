import transferIcon from "../../../assets/svg/transfer-dark.svg";
// import cardIcon from "../../../assets/svg/credit-dark.svg";
import walletIcon from "../../../assets/svg/wallet-dark.svg";
import type { QuickAction } from "../../../types/quickActions";

// label är nycklar i språkfilerna, de översätts med t() i QuickActions.tsx
export const quickActions: QuickAction[] = [
    {
        id: "transaction",
        label: "dashboard.quickActions.transaction",
        icon: transferIcon,
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
    //     id: "newAccount",
    //     label: "dashboard.quickActions.newAccount",
    //     icon: cardIcon,
    //     size: "small",
    // },
    //     {
    //     id: "aiBuddy",
    //     label: "dashboard.quickActions.aiBuddy",
    //     icon: aiBuddyIcon,
    //     size: "small",
    // },
                        
]