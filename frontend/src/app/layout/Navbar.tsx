import { NavLink } from "react-router-dom"

const navLinks = [
    {
        name: "Översikt",
        to: "/dashboard" ,
        iconWhite: "/svg/home-white.svg",
        iconDark: "/svg/home-dark.svg"
    },
        {
        name: "Konton",
        to: "/transactions" ,
        iconWhite: "/svg/profile-white.svg",
        iconDark: "/svg/profile-dark.svg"
    },
    {
        name: "Skatterapporter",
        to: "/taxreport" ,
        iconWhite: "/svg/taxes-white.svg",
        iconDark: "/svg/taxes-dark.svg"
    },
    {
        name: "Vanliga frågor",
        to: "/faq" ,
        iconWhite: "/svg/faq-white.svg",
        iconDark: "/svg/faq-dark.svg"
    },
    {
        name: "Logga ut",
        to: "/login" ,
        iconWhite: "/svg/logout-white.svg",
        iconDark: "/svg/logout-dark.svg"
    },
]


export default function Navbar() {
    return (
        <nav className="bg-brand lg:border-r border-border lg:col-span-2 p-6">
            <h2 className="text-white font-bold">nordiska<span className="text-accent">.</span></h2>
            <ul className="pt-6 text-medium">
                {navLinks.map((navLink) => (
                    <li key={navLink.to} className= "w-full">
                        <NavLink to={navLink.to} className={({ isActive }) => 
                            `group flex w-full p-3  mt-1 rounded-default transition-colors duration-150 ${isActive 
                            ? "bg-white text-brand"
                            : "text-white hover:bg-white hover:text-brand" }
                            `}
                        >
                            {({ isActive }) => (
                                <>
                                    <img src={navLink.iconWhite}
                                        alt=""
                                        className={`size-5 shrink-0 ${
                                            isActive ? "hidden" : "block group-hover:hidden"}`}
                                    />
                                    <img src={navLink.iconDark}
                                        alt=""
                                        className={`size-5 shrink-0 ${
                                            isActive ? "block" : "hidden group-hover:block"}`}
                                    />
                                    <span className="ml-3"> {navLink.name}</span>
                                </>
                            )} 
                        </NavLink>
                    </li>
                ))}
            </ul>
        </nav>
    )
}