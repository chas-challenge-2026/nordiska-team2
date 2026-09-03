import Card from "../../../components/cards/Card";
import { Link } from "react-router-dom";


const currencyFormatter = new Intl.NumberFormat("sv-SE", {
    style: "currency",
    currency: "SEK",
});

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
    return (
        <Card 
            title={name}
            subtitle={accountNumber}
            headerVariant="primary"
        >
            <p className="text-small text-muted">
                Tillgängligt saldo
            </p>
            <p className="text-balance font-bold text-brand"> 
                {currencyFormatter.format(balance)}
            </p>
            {interest !== undefined &&(
                <p className="text-muted text-small">
                    Ränta {interest} %
                </p>
            )} {to 
                ? (
                    <Link 
                        to={to} 
                        aria-label={`Visa detaljer för ${name}`} 
                        className="mt-auto self-end text-large font-bold">
                            ››
                    </Link>)
                : (
                    <span aria-hidden="true"
                        className="mt-auto self-end text-large font-bold">
                            ››
                    </span>
                )}
        </Card>
    )
}