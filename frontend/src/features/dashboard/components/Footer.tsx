import { Link } from "react-router-dom"

type FooterLinkProps = {
    label: string,
    to: string
}

const footerLinks: FooterLinkProps[] = [
    {
        label: "Säkerhet",
        to: "#"
    },
    {
        label: "Integritet",
        to: "#"
    },
    {
        label: "Villkor",
        to: "#"
    }
]


export default function Footer(){
    return (
        <footer className="text-xsmall text-muted mt-2 lg:col-span-10 mt-5">
            <div className="flex gap-10 justify-center">
                {footerLinks.map((footerLink) => (
                    <Link
                    key={footerLink.label}
                    to={footerLink.to}>
                        {footerLink.label}
                    </Link>
                ))}
            </div>
        
            <div className="flex justify-center mt-2">
                © {new Date().getFullYear()} Nordiska . </div>
        </footer>
    )
}