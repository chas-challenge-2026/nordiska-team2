type TextareaProps = {
    value?: string;
    onChange:(value: string) => void;
    placeholder?: string;
    className?: string;
}

export default function TextareaInput({
    value,
    onChange,
    placeholder,
    className="",
}: TextareaProps) {
    return (
        <>
            <textarea 
                value={value}
                onChange={(e) => onChange(e.target.value)}
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