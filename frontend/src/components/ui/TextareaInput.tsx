type TextareaProps = {
    placeholder?: string;
    className?: string;
}

export default function TextareaInput({
    placeholder,
    className="",
}: TextareaProps) {
    return (
        <>
            <textarea 
                placeholder={placeholder}
                className={`text-small border border-muted rounded-default
                            focus:bg-white
                            p-2 pl-2
                            w-full
                            ${className}`}  
            />
            
        </>
    )
}