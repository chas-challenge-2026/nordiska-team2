import { Link } from "react-router-dom"
import Card from "../../../components/cards/Card"
import { useTranslation } from "react-i18next"

export default function FaqOverview() {
    const { t } = useTranslation()
    return (
        <Card title={t("dashboard.needHelp")}
            headerVariant="secondary">
            {/* -m-5 tar bort kortets padding så att raden blir klickbar i hela bredden */}
            <ul className="-m-5 divide-y divide-border-light">
                <li>
                    <Link to="/faq"
                          className="flex items-center justify-between px-5 py-3
                                     text-small transition hover:bg-background
                                     focus-visible:outline-2 focus-visible:outline-brand">
                        {t("dashboard.mostCommonQuestions")}
                        <span aria-hidden="true" className="text-brand-text">›</span>
                    </Link>
                </li>
            </ul>
        </Card>
    )
}
