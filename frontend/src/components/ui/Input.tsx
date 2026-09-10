type InputFieldProps = {
    placeholder?: string;
    className?: string
}



export default function InputField({
        placeholder, 
        className="", 
        }: InputFieldProps){
    return(
        <>

        <input type="text"
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