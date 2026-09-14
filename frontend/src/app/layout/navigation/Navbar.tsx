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
    <nav className="border-border bg-brand p-6 
                    lg:col-span-2 lg:border-r lg:sticky lg:top-0 lg:h-dvh 
                    lg:self-start hidden lg:block">
        <h2 className="font-bold text-white">
            nordiska<span className="text-accent">.</span>
        </h2>

        <ul className="pt-6 text-medium">
            {navigationItems.map((item) => (
                <NavItem key={item.to} item={item} />
            ))}
            <li className="mt-8">
                <button
                    onClick={handleLogout}
                    className="group flex w-full rounded-default p-3 text-white hover:bg-white hover:text-brand transition-colors duration-150"
                >
                    <img src={logoutWhite} alt="" aria-hidden="true" className="size-5 shrink-0" />
                    <span className="ml-3">Logga ut</span>
                </button>
            </li>
        </ul>
    </nav>
);
}