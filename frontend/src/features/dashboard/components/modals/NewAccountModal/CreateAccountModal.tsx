import { useMutation, useQueryClient } from "@tanstack/react-query"
import Button from "../../../../../components/ui/Button"
import Modal from "../../../../../components/ui/Modal"
import { apiClient } from "../../../../../client"
import { useAlert } from "../../../../../hooks/useAlert"

type NewAccountModalProps = {
    isOpen: boolean;
    onClose: () => void;
};

export default function CreateAccountModal({ isOpen, onClose }: NewAccountModalProps ) {
    const queryClient = useQueryClient();
    const { showAlert } = useAlert();

    const createAccountMutation = useMutation({
        mutationFn: () => apiClient.post("/accounts"),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ["accounts"] });
            showAlert({ type: "success", title: "Klart!", message: "Kontot är skapat." });
            onClose();
        },
    })
    return(
        <Modal title="Skapa nytt konto"
                isOpen={isOpen} 
                onClose={onClose} >
            <div className="border border-border rounded-card p-3 pt-7 mt-3 mb-5">
                <p className="text-small text-muted mb-5">Ett nytt sparkonto skapas automatiskt med ett nytt kontonummer och standardränta.</p>
            </div>
            <div className="flex justify-end gap-3"> 
                <Button 
                    label="Avbryt"
                    variant="cancel"
                    onClick={onClose}
                />
                <Button 
                    label={createAccountMutation.isPending ? "Skapar..." : "Skapa konto"} 
                    variant="secondary"
                    onClick={() => createAccountMutation.mutate()}
                    disabled={createAccountMutation.isPending}
                    />
                {createAccountMutation.isError && (
                    <p className="text-xsmall text-cancel mt-2">Kunde inte skapa kontot. Försök igen lite senare. </p>
                )}
            </div>

        </Modal>
    )
}