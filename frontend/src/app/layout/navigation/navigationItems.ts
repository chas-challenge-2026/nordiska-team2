import homeWhite from "../../../assets/svg/home-white.svg";
import homeDark from "../../../assets/svg/home-dark.svg";
import profileWhite from "../../../assets/svg/profile-white.svg";
import profileDark from "../../../assets/svg/profile-dark.svg";
import taxesWhite from "../../../assets/svg/taxes-white.svg";
import taxesDark from "../../../assets/svg/taxes-dark.svg";
import faqWhite from "../../../assets/svg/faq-white.svg";
import faqDark from "../../../assets/svg/faq-dark.svg";
import settingsWhite from "../../../assets/svg/settings-white.svg"
import settingsDark from "../../../assets/svg/settings-dark.svg"


// name och mobileLabel är nycklar i språkfilerna (t.ex. "nav.overview").
// De översätts med t() där de visas, i NavItem och MobileNavbar.
export type NavigationItem = {
    name: string;
    mobileLabel?: string,
    to: string;
    iconWhite: string;
    iconDark: string;
    startsSection?: boolean;
    showInMobileNav?: boolean;
    onCLick?: () => void; 
};

export const navigationItems: NavigationItem[] = [
 {
        name: "nav.overview",
        to: "/dashboard" ,
        iconWhite: homeWhite,
        iconDark: homeDark,
        showInMobileNav: true,
    },
        {
        name: "nav.accounts",
        to: "/transactions" ,
        iconWhite: profileWhite,
        iconDark: profileDark,
        showInMobileNav: true,
    },
    {
        name: "nav.documents",
        to: "/taxreport" ,
        iconWhite: taxesWhite,
        iconDark: taxesDark,
    },
    {
        name: "nav.faq",
        mobileLabel: "nav.faqShort",
        to: "/faq" ,
        iconWhite: faqWhite,
        iconDark: faqDark,
        showInMobileNav: true
    },
    {
        name: "nav.settings",
        mobileLabel: "nav.settingsShort",
        to: "/settings" ,
        iconWhite: settingsWhite,
        iconDark: settingsDark,
        startsSection: true,
        showInMobileNav: true,
    },
    // {
    //     // name: "Logga ut",
    //     // to: "/login" ,
    //     iconWhite: logoutWhite,
    //     iconDark: logoutDark,
    // },
];