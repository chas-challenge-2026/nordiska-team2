import { NavLink } from "react-router-dom";
import type { NavigationItem } from "./navigationItems";

type NavItemProps = {
    item: NavigationItem;
};

export default function NavItem ({ item }: NavItemProps) {
    return (
        <li className={`w-full ${item.startsSection ? "mt-8" : "mt-1"}`}>
            <NavLink
                to={item.to}
                className={({ isActive }) =>
                    `group flex w-full rounded-default p-3
                    transition-colors duration-150 ${
                        isActive
                            ? "bg-white text-brand"
                            : "text-white hover:bg-white hover:text-brand"
                    }`
                }
            >
                {({ isActive }) => (
                    <>
                        <img
                            src={item.iconWhite}
                            alt=""
                            aria-hidden="true"
                            className={`size-5 shrink-0 ${
                                isActive
                                    ? "hidden"
                                    : "block group-hover:hidden"
                            }`}
                        />

                        <img
                            src={item.iconDark}
                            alt=""
                            aria-hidden="true"
                            className={`size-5 shrink-0 ${
                                isActive
                                    ? "block"
                                    : "hidden group-hover:block"
                            }`}
                        />
                        <span className="ml-3">{item.name}</span>
                    </>
                )}
            </NavLink>
        </li>
    );
}