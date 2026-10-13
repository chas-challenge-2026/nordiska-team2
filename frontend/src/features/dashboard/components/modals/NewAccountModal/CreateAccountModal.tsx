import { useMutation, useQueryClient } from "@tanstack/react-query"
import { useTranslation } from "react-i18next"
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
    const { t } = useTranslation();

    const createAccountMutation = useMutation({
        mutationFn: () => apiClient.post("/accounts"),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ["accounts"] });
            showAlert({ type: "success", title: t("common.done"), message: t("createAccountModal.done") });
            onClose();
        },
    })
    return(
        <Modal title={t("createAccountModal.title")}
                isOpen={isOpen}
                onClose={onClose} >
            <div className="border border-border rounded-card p-3 pt-7 mt-3 mb-5">
                <p className="text-small text-muted mb-5">{t("createAccountModal.description")}</p>
            </div>
            <div className="flex justify-end gap-3">
                <Button
                    label={t("common.cancel")}
                    variant="cancel"
                    onClick={onClose}
                />
                <Button
                    label={createAccountMutation.isPending ? t("createAccountModal.creating") : t("createAccountModal.create")}
                    variant="secondary"
                    onClick={() => createAccountMutation.mutate()}
                    disabled={createAccountMutation.isPending}
                    />
                {createAccountMutation.isError && (
                    <p className="text-xsmall text-cancel mt-2">{t("createAccountModal.createFailed")}</p>
                )}
            </div>

        </Modal>
    )
}
