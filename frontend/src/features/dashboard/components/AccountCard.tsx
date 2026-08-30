import Card from "../../../components/cards/Card";

type AccountCardProps = {
    name: string;
    accountNumber: string; 
    balance: string; 
}

export default function AccountCard({
    name, 
    accountNumber,
    balance,
}: AccountCardProps) {
    return (
        <Card 
            title={name}
            subtitle={accountNumber}
        >
            <p className="text-small text-muted">
                Tillgängligt saldo
            </p>
            <p className="text-balance font-blod text-brand"> 
                {balance} SEK
            </p>
        </Card>
    )
}