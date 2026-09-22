import { useId, type ReactNode } from "react";
import { Link } from "react-router-dom";

type CardHeaderVariant = "primary" | "secondary" | "noHeader" ;

const headerVariantClasses: Record<CardHeaderVariant, string> = {
    primary: "bg-brand text-white",
    secondary: "bg-white text-foreground",
    noHeader: "border-none"
};

type CardProps = {
    title?: string;
    subtitle?: string;
    children: ReactNode;
    className?: string;
    headerVariant?: CardHeaderVariant;
    to?: string;
}

export default function Card({
    title, 
    subtitle, 
    children, 
    className = "",
    headerVariant ="primary",
    to,
}: CardProps){
    const titleId = useId()
    const subtitleId = useId();

    const sharedClassName = `flex flex-col overflow-hidden 
                        w-full rounded-default 
                        border border-border bg-card 
                        shadow-sm ${className}`;

    const content = (
        <>
            <header className={`
                flex flex-col gap-1
                border-b border-border-light 
                px-3 py-4 
                ${headerVariantClasses[headerVariant]}`}>
                
                <h2 
                    id={titleId}
                    className="text-medium font-semibold">
                        {title}
                </h2>

                {subtitle && (
                    <p 
                    id={subtitleId}
                    className="text-small opacity-85"
                    >
                        {subtitle}
                    </p>
                )}
            </header>

            <div className="flex flex-1 flex-col p-3 text-foreground">
                {children}
            </div>
        </>
    );
    if (to) {
        return (
            <Link to={to} aria-label={title} className={sharedClassName}>
                {content}
            </Link>
        );
    }

    return (
        <section aria-labelledby={titleId} aria-describedby={subtitle ? subtitleId : undefined} className={sharedClassName}>
            {content}
        </section>
    );
}



