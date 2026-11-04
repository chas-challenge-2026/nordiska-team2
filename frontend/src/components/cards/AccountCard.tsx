import { Lottie } from "lottie-react";
import Card from "./Card";
import { useTranslation } from "react-i18next";
import { useFormatters } from "../../language/useFormatters";

type AccountCardProps = {
    name: string;
    accountNumber: string;
    balance?: number;
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
            className="h-full hover:-translate-y-2 transition duration-150"
            to={to}
        >
            <p className="text-small text-muted">
               {balance !== undefined? t("accountCard.availableBalance") : t("accountCard.warmingUp")}
            </p>
            <p className="text-title font-bold text-brand-text">
                {balance !== undefined ? (
                    currencyFormatter.format(balance)
                ) : (
                    <span className="inline-block size-8 overflow-hidden align-middle">
                        <Lottie
                            src="https://fonts.gstatic.com/s/e/notoemoji/latest/1f9ad/lottie.json"
                            loop
                            autoplay
                            className="size-full"
                        />
                    </span>
                )}
            </p>
            {interest !== undefined &&(
                <p className="text-muted text-small">
                    {t("accountCard.interest", { rate: interest })}
                </p>
            )} 
        </Card>
    )
}