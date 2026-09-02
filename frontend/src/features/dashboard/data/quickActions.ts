import transactionIcon from "../../../assets/svg/transitions-dark.svg";
import downloadIcon from "../../../assets/svg/download-dark.svg";
import bankIdIcon from "../../../assets/svg/bankid-dark.svg";
import cardIcon from "../../../assets/svg/credit-dark.svg";
import walletIcon from "../../../assets/svg/wallet-dark.svg";
import aiBuddyIcon from "../../../assets/svg/ai-buddy-dark.svg";

export type QuickAction = {
    id: string;
    label: string;
    to: string;
    icon: string;
    size?: "xsmall" | "small" | "medium" | "large";
};

export const quickActions: QuickAction[] = [
    {
        id: "transactions",
        label: "Insättning/Uttag",
        to: "/transactions",
        icon: transactionIcon,
        size: "small",
    },
        {
        id: "payments",
        label: "Betala räkningar",
        to: "/taxreport",
        icon: walletIcon,
        size: "small",
    },
        {
        id: "download",
        label: "Ladda ner skatterapp.",
        to: "/taxreport",
        icon: downloadIcon,
        size: "xsmall",
    },
        {
        id: "bank-id",
        label: "BankID",
        to: "/transactions",
        icon: bankIdIcon,
        size: "small",
    },
        {
        id: "transactions",
        label: "Ansök om lån",
        to: "/transactions",
        icon: cardIcon,
        size: "small",
    },
        {
        id: "ai-buddy",
        label: "Ai Kompis",
        to: "/transactions",
        icon: aiBuddyIcon,
        size: "small",
    },
                        
]