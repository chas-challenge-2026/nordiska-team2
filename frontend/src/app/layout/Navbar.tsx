import { NavLink } from "react-router-dom"

const navLinks = [
    {
        name: "Översikt",
        to: "/dashboard" ,
    },
        {
        name: "Konton",
        to: "/transactions" ,
    },
    {
        name: "Skatterapporter",
        to: "/taxreport" ,
    },
    {
        name: "Vanliga frågor",
        to: "/faq" ,
    },
    {
        name: "Logga ut",
        to: "/login" ,
    },
]


export default function Navbar() {
    return (
        <nav className="lg:border-r border-border lg:col-span-2 p-6">
            <h2 className="text-brand">nordiska<span className="text-accent">.</span></h2>
            <ul className="pt-6 text-medium">
                {navLinks.map((navLink) => (
                    <li key={navLink.to} className= "w-full">
                        <NavLink to={navLink.to} className={({ isActive }) => 
                            `flex w-full p-3 mt-1 rounded-default transition-colors duration-150 ${isActive 
                            ? "bg-brand text-white"
                            : "text-muted hover:bg-brand hover:text-white" }
                            `}
                        >
                            {navLink.name}  
                        </NavLink>
                    </li>
                ))}
            </ul>
        </nav>
    )
}