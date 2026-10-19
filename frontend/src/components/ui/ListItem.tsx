import type { ReactNode } from "react";

type ListItemProps = {
    title: string;
    subtitle?: string;
    right?: ReactNode;
};

export default function ListItem ({
    title,
    subtitle,
    right,
}: ListItemProps) {
    return(
            <li className="grid grid-cols-1 py-2
                            grid-cols-[minmax(0,1fr)_auto]
                            sm:items-start sm:gap-4 sm:px-2">
                <div className="min-w-0">
                    <p className="truncate text-small sm:text-medium">{title} </p>
                    <p className="text-xsmall text-muted">{subtitle}</p>
                </div>
                {right}
            </li>
    )
}