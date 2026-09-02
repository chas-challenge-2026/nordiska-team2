import homeWhite from "../../../assets/svg/home-white.svg";
import homeDark from "../../../assets/svg/home-dark.svg";
import profileWhite from "../../../assets/svg/profile-white.svg";
import profileDark from "../../../assets/svg/profile-dark.svg";
import taxesWhite from "../../../assets/svg/taxes-white.svg";
import taxesDark from "../../../assets/svg/taxes-dark.svg";
import faqWhite from "../../../assets/svg/faq-white.svg";
import faqDark from "../../../assets/svg/faq-dark.svg";
import logoutWhite from "../../../assets/svg/logout-white.svg";
import logoutDark from "../../../assets/svg/logout-dark.svg";
import settingsWhite from "../../../assets/svg/settings-white.svg"
import settingsDark from "../../../assets/svg/settings-dark.svg"

export type NavigationItem = {
    name: string;
    to: string;
    iconWhite: string;
    iconDark: string;
    startsSection?: boolean;
};

export const navigationItems: NavigationItem[] = [
 {
        name: "Översikt",
        to: "/dashboard" ,
        iconWhite: homeWhite,
        iconDark: homeDark,
    },
        {
        name: "Konton",
        to: "/transactions" ,
        iconWhite: profileWhite,
        iconDark: profileDark,
    },
    {
        name: "Skatterapporter",
        to: "/taxreport" ,
        iconWhite: taxesWhite,
        iconDark: taxesDark,
    },
    {
        name: "Vanliga frågor",
        to: "/faq" ,
        iconWhite: faqWhite,
        iconDark: faqDark,
    },
    {
        name: "Inställningar",
        to: "/faq" ,
        iconWhite: settingsWhite,
        iconDark: settingsDark,
        startsSection: true,
    },
    {
        name: "Logga ut",
        to: "/login" ,
        iconWhite: logoutWhite,
        iconDark: logoutDark,
    },
];