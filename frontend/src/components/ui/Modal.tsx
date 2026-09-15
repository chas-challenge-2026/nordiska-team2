import type { ReactNode } from "react";
import { useEffect, useId, useRef } from "react";
import Button from "./Button";


type ModalSizeVariant = "small" | "medium" | "large";
const ModalSizes: Record<ModalSizeVariant, string> = {
    small: "w-100 max-h-1/4",
    medium: "w-1/2 max-h-3/4",
    large: "w-full max-h-full"
}


type ModalProps = {
    title: string; 
    children: ReactNode
    className?: string;
    size?: ModalSizeVariant;
    isOpen: boolean;
    onClose: () => void;
}

export default function Modal({title, 
                            children="", 
                            className,
                            size="medium",
                            isOpen,
                            onClose}: ModalProps) {
    const titleId = useId();
    const dialogRef = useRef<HTMLDialogElement>(null)

    useEffect(() => {
        const dialog = dialogRef.current;
        if (!dialog) return;
        if (isOpen && !dialog.open) {
            dialog.showModal();
        }
        if (!isOpen && dialog.open) {
            dialog.close()
        }
    }, [isOpen]);

    return(
        <dialog
            ref={dialogRef}
            aria-labelledby={titleId}
            onCancel={onClose}
             className={`
                        text-small border border-border 
                        p-3 rounded-default shadow-sm
                        backdrop:bg-black/30
                        m-auto overflow-y-auto
                        ${className} ${ModalSizes[size]}`}
                        >
                <div className="flex justify-between items-center">
                    <h2 id={titleId}
                        className="text-large">{title}</h2>

                    <Button label="x"
                        onClick={onClose}
                        variant="cancel"
                        className="w-8 h-8
                                    flex justify-center items-center"
                    />
                </div>
            {children}
        </dialog>
    )
}
