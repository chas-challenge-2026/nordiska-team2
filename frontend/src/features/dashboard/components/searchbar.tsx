import { useState } from "react"
import InputField from "../../../components/ui/Input"
import Button from "../../../components/ui/Button"


export default function SearchBar() {
    const [query, setQuery] = useState("")
    
    function handleSearch(){
        console.log("Söker efter", query);
    }

    return (
        <div className="flex items-center">
        <InputField 
            value={query}
            onChange={setQuery}
            placeholder="Sök ..."/>
        <Button 
            label="Sök" 
            onClick={handleSearch}
            />
        </div>
    )
}