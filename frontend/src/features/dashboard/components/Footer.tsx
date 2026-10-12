import { Link } from "react-router-dom"
import { useTranslation } from "react-i18next"

type FooterLinkProps = {
    label: string,
    to: string
}

const footerLinks: FooterLinkProps[] = [
    {
        label: "footer.security",
        to: "#"
    },
    {
        label: "footer.privacy",
        to: "#"
    },
    {
        label: "footer.terms",
        to: "#"
    }
]


export default function Footer(){
    const { t } = useTranslation()
    return (
        <footer className="text-xsmall text-muted mt-2 lg:col-span-10 mt-5">
            <div className="flex gap-10 justify-center">
                {footerLinks.map((footerLink) => (
                    <Link
                    key={footerLink.label}
                    to={footerLink.to}>
                        {t(footerLink.label)}
                    </Link>
                ))}
            </div>
        
            <div className="flex justify-center mt-2">
                © {new Date().getFullYear()} Nordiska . </div>
        </footer>
    )
}