import NavItem from "./NavItem";
import { navigationItems } from "./navigationItems";

export default function Navbar() {
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
            </ul>
        </nav>
    );
}