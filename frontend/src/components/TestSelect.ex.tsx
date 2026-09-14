import Button from "./ui/Button";
import SelectOptions from "./ui/Select";
import { useState } from "react";

const options = [
    {value: "Mock1", label: "Mock1"},
    {value: "Mock2", label: "Mock2"},
    {value: "Mock3", label: "Mock3"},
]

export default function TestSelect(){
    const [selectedOption, setSelectedOption] = useState<{ value: string; label: string;} | null>(null);
    
    function handleConfirm() {
        if(selectedOption) {
            alert(`Du har valt ${selectedOption.label}`)
        }
    }
    function handleCancel(){
        alert("Du har valt att avbryta")
    }

     return(
        <>
            <SelectOptions 
                value = {selectedOption} 
                onChange={setSelectedOption}
                options={options}
                placeholder="Välj"
            />
            <div className="flex justify-end">
                <Button label="Avbryt" variant="cancel" onClick={handleCancel} />
                <Button label="OK" variant="success" onClick={handleConfirm} />
            </div>
        </>
    )
}