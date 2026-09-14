export type AlertType = "success" | "error" | "warning" | "info"

const AlertTypesOptions = {
    success: "bg-success text-white text-semibold border border-brand text-small",
    error: "bg-cancel text-white text-semibold border border-brand text-small",
    warning: "bg-card text-white text-semibold border border-brand text-small",
    info: "bg-card border border-border text-small"
}

type AlertProps = {
    title?: string,
    message: string,
    type: AlertType,
    visible?: boolean,
    className?: string,
}

export default function Alert({
    title,
    message,
    type = "info",
    visible= true,
    className="",
    }: AlertProps) {
    return(
        <>
            <div title={title}
                className={`${AlertTypesOptions[type]} ${visible ? "opacity-100": "opacity-0"}
                        p-5 rounded-default border border-border
                        ${className}`}
                >
                <p>{message}</p>
            
            </div>    
        </>
    )
}