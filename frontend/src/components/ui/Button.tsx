type buttonVariant = "primary" | "secondary" | "success" | "cancel";

const buttonVariantClasses: Record<buttonVariant, string> = {
    primary: "bg-white text-brand text-small hover:bg-border hover:border-brand",
    secondary: "bg-brand text-white text-small hover:opacity-90",
    success: "bg-success text-white text-small hover:opacity-90 hover:border-brand",
    cancel: "bg-cancel text-white text-small hover:bg-opacity-90"
}

type ButtonProps = {
    label: string; 
    variant?: buttonVariant;
    className?: string;
    onClick: () => void;
    disabled?: boolean;
}

export default function Button({
        label, 
        variant="primary", 
        onClick,
        className="",
        disabled=false
    }: ButtonProps){
    return(
        <>
            <button 
                type="button"
                className={`p-2 m-2 cursor-pointer 
                            border border-border shadow-sm rounded-default
                            transition-transform duration-150 active:scale-98
                            ${buttonVariantClasses[variant]} ${className}`}
                disabled={disabled}
                onClick={onClick}
                >
                {label}
            </button>
        </>
    )
}