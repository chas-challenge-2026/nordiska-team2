import NavItem from "./NavItem";
import { navigationItems } from "./navigationItems";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../../hooks/useAuth";
import { apiClient } from "../../../client"
import logoutWhite from "../../../assets/svg/logout-white.svg"



export default function Navbar() {
const auth = useAuth()
const navigate = useNavigate()

async function handleLogout() {
    try {
        await apiClient.post('/auth/logout')
    } catch (error) {

    }
    auth.setAccessToken(null)
    navigate('/login')
}

   return (
    <nav className="border-border bg-sidebar p-6
                    lg:col-span-2 lg:border-r lg:sticky lg:top-0 lg:h-dvh 
                    lg:self-start hidden lg:block">
        <h2 className="font-bold text-white">
            nordiska<span className="text-accent">.</span>
        </h2>

        <ul className="pt-6 text-medium 
                        flex flex-col flex-1 h-full">
            {navigationItems.map((item) => (
                <NavItem key={item.to} item={item} />
            ))}
            <li className="mb-5">
                <button
                    onClick={handleLogout}
                    className="group flex w-full rounded-default p-3 text-white transition-colors duration-150 hover:bg-card hover:text-brand dark:hover:bg-white/10 dark:hover:text-white"
                >
                    <img src={logoutWhite} alt="" aria-hidden="true" className="size-5 shrink-0" />
                    <span className="ml-3">Logga ut</span>
                </button>
            </li>
        </ul>
    </nav>
);
}