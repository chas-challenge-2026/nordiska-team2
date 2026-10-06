import { NavLink } from "react-router-dom";
import { navigationItems } from "./navigationItems";
import { useSettings } from "../../../hooks/useSettings";

const mobileItems = navigationItems.filter(
    (item) => item.showInMobileNav,
);

export default function MobileNavbar() {
    const { theme } = useSettings();

    return (
        <nav
            aria-label="Mobil navigation"
            className="fixed inset-x-0 bottom-0 z-50
                       border-t border-border bg-sidebar
                       pb-[env(safe-area-inset-bottom)]
                       lg:hidden"
        >
            <ul className="grid grid-cols-4">
                {mobileItems.map((item) => (
                    <li key={item.to}>
                        <NavLink
                            to={item.to}
                            className={({ isActive }) =>
                                `flex min-h-16 flex-col
                                items-center justify-center gap-1
                                px-2 py-2 text-xsmall
                                ${isActive
                                    ? "bg-card text-brand dark:bg-brand dark:text-white"
                                    : "text-white"
                                }`
                            }
                        >
                            {({ isActive }) => {
                                // Mörk ikon bara när knappen är vit (aktiv i ljust läge)
                                const useDarkIcon = isActive && theme === "light";

                                return (
                                    <>
                                        <img
                                            src={useDarkIcon ? item.iconDark : item.iconWhite}
                                            alt=""
                                            aria-hidden="true"
                                            className="size-5"
                                        />
                                        <span className="text-center">
                                            {item.mobileLabel ?? item.name}
                                        </span>
                                    </>
                                );
                            }}
                        </NavLink>
                    </li>
                ))}
            </ul>
        </nav>
    );
}