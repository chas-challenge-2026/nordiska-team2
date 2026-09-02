import NavItem from "./NavItem";
import { navigationItems } from "./navigationItems";

export default function Navbar() {
    return (
        <nav className="border-border bg-brand p-6 lg:col-span-2 lg:border-r">
            <h2 className="font-bold text-white">
                nordiska<span className="text-accent">.</span>
            </h2>

            <ul className="pt-6 text-medium">
                {navigationItems.map((item) => (
                    <NavItem key={item.to} item={item} />
                ))}
            </ul>
        </nav>
    );
}