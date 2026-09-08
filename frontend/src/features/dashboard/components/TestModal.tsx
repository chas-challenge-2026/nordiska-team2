import Modal from "../../../components/ui/Modal"    

type TestModalProps = {
    isOpen: boolean;
    onClose: () => void;
};


export default function TestModal({
    isOpen,
    onClose,
}: TestModalProps) {
    return(
        <Modal title="Titel"
                isOpen ={isOpen}
                onClose={onClose}
        >
            <div className="border border-border rounded-default
                            w-full h-full p-3"
                            >
                               <p> Innehåll </p>
                            <br />
                            <br />
                            <br />
                            <br />
                                <p> . </p>
                            </div>
        </Modal>

    )
}
                    
                    
