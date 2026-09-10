import InputField from "../../../components/ui/Input"
import Button from "../../../components/ui/Button"

function handleSearch(){
    console.log("Sök sök sök ...")
}

export default function SearchBar() {
    return (
        <div className="flex items-center">
        <InputField 
            placeholder="Sök"/>
        <Button 
            label="Sök" 
            onClick={handleSearch}
            />
        </div>
    )
}