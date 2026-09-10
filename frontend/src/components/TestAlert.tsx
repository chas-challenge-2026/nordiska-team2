import { useAlert } from "../hooks/useAlert";
import Button from "./ui/Button";



export default function TestAlert(){
    const { showAlert } = useAlert(); 
    
    function handleAlert(){
        showAlert({
            type: "info", 
            title: "Info", 
            message: "Info-test-text"
        });
    } 
    
    return(
        <>
            <Button label="Alert"
                onClick={handleAlert} />
        </>
    )
}