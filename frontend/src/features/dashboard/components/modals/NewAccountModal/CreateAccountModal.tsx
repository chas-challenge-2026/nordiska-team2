import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useTranslation } from "react-i18next"
import Button from "../../../../../components/ui/Button"
import Modal from "../../../../../components/ui/Modal"
import { apiClient } from "../../../../../client"
import { useAlert } from "../../../../../hooks/useAlert"
import { apiErrorMessage } from "../../../../../lib/apiErrorMessage"

type NewAccountModalProps = {
    isOpen: boolean;
    onClose: () => void;
};

export default function CreateAccountModal({ isOpen, onClose }: NewAccountModalProps ) {
    const queryClient = useQueryClient();
    const { showAlert } = useAlert();
    const { t } = useTranslation();

    const createAccountMutation = useMutation({
        mutationFn: () => apiClient.post("/accounts"),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ["accounts"] });
            showAlert({ type: "success", title: t("common.done"), message: t("createAccountModal.done") });
            onClose();
        },
    })

    // Closes the modal and forgets the error from the previous attempt,
    // so it isn't still showing the next time the modal opens.
    function handleClose() {
        createAccountMutation.reset();
        onClose();
    }

    return(
        <Modal title={t("createAccountModal.title")}
                isOpen={isOpen}
                onClose={handleClose} >
            <div className="border border-border rounded-card p-3 pt-7 mt-3 mb-5">
                <p className="text-small text-muted mb-5">{t("createAccountModal.description")}</p>
            </div>
            
            {createAccountMutation.isError && (
                <p role="alert" className="text-xsmall text-cancel text-right mb-3">
                    {apiErrorMessage(createAccountMutation.error, t)}
                </p>
            )}

            <div className="flex justify-end gap-3">
                <Button
                    label={t("common.cancel")}
                    variant="cancel"
                    onClick={handleClose}
                />
                <Button
                    label={createAccountMutation.isPending ? t("createAccountModal.creating") : t("createAccountModal.create")}
                    variant="secondary"
                    onClick={() => createAccountMutation.mutate()}
                    disabled={createAccountMutation.isPending}
                />
            </div>
        </Modal>
    )
}
