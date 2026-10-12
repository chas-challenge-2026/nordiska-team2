import Card from "./Card";
import { useTranslation } from "react-i18next";
import { useFormatters } from "../../language/useFormatters";

type AccountCardProps = {
    name: string;
    accountNumber: string; 
    balance: number; 
    interest?: number;
    to?: string;
}

export default function AccountCard({
    name, 
    accountNumber,
    balance,
    interest,
    to,
}: AccountCardProps) {
    const { t } = useTranslation();
    const { currency: currencyFormatter } = useFormatters();
    return (
        <Card 
            title={name}
            subtitle={accountNumber}
            headerVariant="primary"
            className="hover:-translate-y-2 transition duration-150"
            to={to}
        >
            <p className="text-small text-muted">
                {t("accountCard.availableBalance")}
            </p>
            <p className="text-title font-bold text-brand-text"> 
                {currencyFormatter.format(balance)}
            </p>
            {interest !== undefined &&(
                <p className="text-muted text-small">
                    {t("accountCard.interest", { rate: interest })}
                </p>
            )} 
        </Card>
    )
}