import { useState } from "react"
import InputField from "../../../components/ui/Input"
import Button from "../../../components/ui/Button"
import { useTranslation } from "react-i18next"


export default function SearchBar() {
    const [query, setQuery] = useState("")
    const { t } = useTranslation()
    
    function handleSearch(){
        console.log("Söker efter", query);
    }

    return (
        <div className=" md:flex  items-center hidden 
                        lg:relative md:absolute md:top-0 md:right-0">
        <InputField 
            value={query}
            onChange={setQuery}
            placeholder={t("dashboard.searchPlaceholder")}/>
        <Button 
            label={t("dashboard.search")} 
            onClick={handleSearch}
            />
        </div>
    )
}