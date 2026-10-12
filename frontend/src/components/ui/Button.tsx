import { type ReactNode } from "react";
type buttonVariant = "primary" | "secondary" | "success" | "cancel" | "dropDown" | "tile" | "quickPick";

const buttonVariantClasses: Record<buttonVariant, string> = {
    primary: "bg-card text-brand-text text-small hover:bg-border hover:border-brand p-2",
    secondary: "bg-brand text-white text-small hover:bg-brand/90 min-w-30 p-2",
    success: "bg-success text-white text-small hover:bg-success/90 hover:border-brand min-w-30 p-2",
    cancel: "bg-cancel text-white text-small hover:bg-cancel/90 min-w-30 p-2",
    dropDown: "text-brand-text text-small bg-card flex w-1/4 items-center justify-between p-2",
    tile: "bg-card text-foreground text-small hover:border-brand-text p-2",
    quickPick: "bg-card text-brand-text text-small hover:bg-border hover:border-brand py-1 px-3",
    
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
                className={`cursor-pointer
                            border border-border shadow-sm rounded-card
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