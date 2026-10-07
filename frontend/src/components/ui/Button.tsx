import { type ReactNode } from "react";
type buttonVariant = "primary" | "secondary" | "success" | "cancel" | "dropDown" | "tile";

const buttonVariantClasses: Record<buttonVariant, string> = {
    primary: "bg-card text-brand-text text-small hover:bg-border hover:border-brand",
    secondary: "bg-brand text-white text-small hover:opacity-90",
    success: "bg-success text-white text-small hover:opacity-90 hover:border-brand",
    cancel: "bg-cancel text-white text-small hover:bg-opacity-90",
    dropDown: "text-brand-text text-small bg-card flex w-1/4 items-center justify-between",
    tile: "bg-card text-foreground text-small hover:border-brand-text"
}

type ButtonProps = {
    label: string; 
    variant?: buttonVariant;
    className?: string;
    onClick: () => void;
    disabled?: boolean;
    icon?: ReactNode;
    children?: ReactNode;
}

export default function Button({
        label, 
        variant="primary", 
        onClick,
        className="",
        disabled=false,
        icon,
        children,
    }: ButtonProps){
    return(
        <>
            <button 
                type="button"
                className={`p-2 cursor-pointer 
                            border border-border shadow-sm rounded-default
                            transition-transform duration-150 active:scale-98
                            ${buttonVariantClasses[variant]} ${className}`}
                disabled={disabled}
                onClick={onClick}
                > 
                {icon}
                {label}
                {children}
            </button>
        </>
    )
}