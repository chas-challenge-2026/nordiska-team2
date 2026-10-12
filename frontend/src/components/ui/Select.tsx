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
                    control: () => `border border-border rounded-card p-1 pl-2 bg-card text-brand-text  ${className}`,
                    menu: () => "border border-border rounded-default bg-card text-foreground mt-0.5 z-50 text-small",
                    option: (state) => `p-2 ${state.isFocused ? "bg-background rounded-default" : ""}`,
                    placeholder: () => "text-muted",
                }}
            />
        </>        
    )
}