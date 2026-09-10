import Select from "react-select"

export type OptionType = {
    value: string,
    label: string,
}

type SelectProps = {
    value: OptionType | null,
    onChange: (option: OptionType | null) => void; 
    options: OptionType[];
    className?: string;
    placeholder?: string;
}

export default function SelectOptions({
    value,
    onChange,
    options,
    className="",
    placeholder,
}: SelectProps){

    return(
        <>
            <Select
                value={value}
                onChange={onChange} 
                options={options} 
                className={`text-small border border-muted rounded-default
                            
                            ${className}`}
                placeholder={placeholder}
            />
        </>        
    )
}