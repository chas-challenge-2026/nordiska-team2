import type { ReactNode } from "react";
import { Link, type LinkProps } from "react-router-dom";

type LinkButtonProps =  LinkProps & {
    variant?: "primary" | "secondary"
    size?: "small" | "medium" | "large"
    icon?: ReactNode;
}

const variantClasses = {
    primary: "bg-white text-brand hover:bg-border",
    secondary: "bg-brand text-white hover:opacity-90",
}

const sizeClasses = {
    small: "px-3 py-2 text-small",
    medium: "px-4 py-2 text-medium",
    large: "px-6 py-4 text-large",
}

export default function LinkButton({
    variant = "primary",
    size = "medium",
    icon,
    children,
    className ="",
    ...linkProps
}: LinkButtonProps) {
    return(
        <Link 
            {...linkProps}
            className={`
                inline-flex flex-col items-center 
                justify-center
                rounded-default
                transition duration-150
                border border-border 
                shadow-sm
                focus-visible:outline-2
                focus-visible:outline-offset-2
                focus-visible:outline-brand
                w-full max-w-37 h-25 gap-2
                ${variantClasses[variant]}
                ${sizeClasses[size]}
                ${className}
            `}
        >
            {icon}
            <span>{children}</span>
        </Link>
    )
}