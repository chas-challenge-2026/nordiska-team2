import { Link } from "react-router-dom"

type FooterLinkProps = {
    label: string,
    to: string
}

const footerLinks: FooterLinkProps[] = [
    {
        label: "Säkerhet",
        to: "/taxreport"
    },
    {
        label: "Integritet",
        to: "/taxreport"
    },
    {
        label: "Villkor",
        to: "/taxreport"
    }
]


export default function Footer(){
    return (
        <div className="text-xsmall text-muted mt-2">
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
        </div>
    )
}