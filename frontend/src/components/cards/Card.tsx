import { useId, type ReactNode } from "react";

type CardProps = {
    title: string;
    subtitle?: string;
    children: ReactNode;
    className?: string;
}

export default function Card({
    title, 
    subtitle,
    children, 
    className = "",
}: CardProps) {
    const titleId = useId()
    const subtitleId = useId();

    return (
        <section
            aria-labelledby={titleId}
            aria-describedby={subtitle ? subtitleId : undefined}
            className={`overflow-hidden rounded-default border border-border bg-card shadow-sm ${className}`}
            >
                <header className="border-b border-border-light px-6 py-4 bg-brand">
                    <h2 
                    id={titleId}
                    className="text-card-title font-semibold text-white">
                        {title}
                    </h2>

                    {subtitle && (
                        <p 
                        id={subtitleId}
                        className="mt-1 text-small text-white"
                        >
                            {subtitle}
                        </p>
                    )}
                </header>

                <div className="p-6 text-foreground">
                    {children}
                </div>
        </section>
    )
}