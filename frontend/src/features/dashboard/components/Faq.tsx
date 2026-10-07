import { Link } from "react-router-dom"
import Card from "../../../components/cards/Card"

export default function FaqOverview() {
    return (
        <Card title="Behöver du hjälp?"
            headerVariant="secondary">
            {/* -m-5 tar bort kortets padding så att raden blir klickbar i hela bredden */}
            <ul className="-m-5 divide-y divide-border-light">
                <li>
                    <Link to="/faq"
                          className="flex items-center justify-between px-5 py-3
                                     text-small transition hover:bg-background
                                     focus-visible:outline-2 focus-visible:outline-brand">
                        Vanligaste frågorna
                        <span aria-hidden="true" className="text-brand-text">›</span>
                    </Link>
                </li>
            </ul>
        </Card>
    )
}
