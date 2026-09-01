import Card from "../../../components/cards/Card";
import { Link } from "react-router-dom";

type AccountCardProps = {
    name: string;
    accountNumber: string; 
    balance: string; 
    interest?: string;
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
        >
            <p className="text-small text-muted">
                Tillgängligt saldo
            </p>
            <p className="text-balance font-bold text-brand"> 
                {balance} SEK
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