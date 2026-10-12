type InputFieldProps = {
    value: string;
    onChange: (value: string) => void;
    placeholder?: string;
    className?: string
    type?: string;
    inputMode?: "text" | "decimal" | "numeric" | "none" | "tel" | "search" | "email" | "url";
}



export default function InputField({
    value,
    onChange,
    placeholder,
    className="",
    type="text",
    inputMode,
    }: InputFieldProps){
        return(
            <>
                <input type={type}
                    inputMode={inputMode}
                    value={value}
                    onChange={(e) => onChange(e.target.value)}
                    placeholder={placeholder}
                    className={`text-small border border-muted rounded-card
                                focus:bg-card
                                p-2 pl-2
                                w-full
                                ${className}`}
                />
            </>
        )
    }