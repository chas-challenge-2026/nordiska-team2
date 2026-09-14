import Card from "./Card";


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
            className="hover:-translate-y-2 transition duration-150"
            to={to}
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
            )} 
        </Card>
    )
}