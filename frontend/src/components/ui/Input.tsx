type InputFieldProps = {
    value: string;
    onChange: (value: string) => void;
    placeholder?: string;
    className?: string
    type?: string;
}



export default function InputField({
    value,  
    onChange,  
    placeholder, 
    className="", 
    type="text",
    }: InputFieldProps){
        return(
            <>
                <input type={type}
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