import Button from "./ui/Button";
import Modal from "./ui/Modal"  
import TextareaInput from "./ui/TextareaInput";  

type TestModalProps = {
    isOpen: boolean;
    onClose: () => void;
};


export default function TestModal({
    isOpen,
    onClose,
}: TestModalProps) {

    function handleConfirmed() {
    alert("Tack för bekräftelsen");
        onClose();
    }
    return(
        <Modal title="Titel"
                isOpen ={isOpen}
                onClose={onClose}
        >
            <div className="border border-border rounded-default
                        w-full h-full p-3"
                >
                    <p className="my-3 ml-1"> Bankärende </p>
                    <TextareaInput 
                        placeholder="Skriv ett meddelande"/>
                    </div>

                    <Button label="Skicka"
                            variant="success"
                            onClick={handleConfirmed}
                    />
        </Modal>
    )
}
                    
                    
