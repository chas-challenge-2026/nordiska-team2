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
                placeholder={placeholder}
                unstyled
                menuPosition="fixed"
                classNames={{
                    control: () => `border border-muted rounded-default p-1 bg-white ${className}`,
                    menu: () => "border border-muted rounded-default bg-white mt-1 z-20 text-small",
                    option: (state) => `p-2 ${state.isFocused ? "bg-border" : ""}`,
                    placeholder: () => "text-muted",
                }}
            />
        </>        
    )
}