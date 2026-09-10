export type AlertType = "success" | "error" | "warning" | "info"

const AlertTypesOptions = {
    success: "bg-success border border-border",
    error: "bg-cancel border border-border",
    warning: "bg-cancel border border-border",
    info: "bg-card border border-border"
}

type AlertProps = {
    title?: string;
    message: string;
    type: AlertType;
    className?: string;
}

export default function Alert({
    title,
    message,
    type = "info",
    className="",
    }: AlertProps) {
    return(
        <>
            <div title={title}
                className={`${AlertTypesOptions[type]}
                        ${className}`}
                >
                <p>{message}</p>
            
            </div>    
        </>
    )
}