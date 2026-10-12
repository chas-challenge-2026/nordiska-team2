import type { ReactNode } from "react";
import { useId } from "react";


type ModalSizeVariant = "small" | "medium" | "large";
const ModalSizes: Record<ModalSizeVariant, string> = {
    small: "w-full max-h-full md:w-100 md:max-h-1/4",
    medium: "w-full max-h-full md:w-1/2 md:max-h-3/4",
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

    if (!isOpen) return null;

    return(
        <div
            onClick={onClose}
            className="fixed inset-x-0 top-0 bottom-[calc(4rem_+_env(safe-area-inset-bottom))] lg:bottom-0 z-50 flex items-center justify-center bg-black/30 dark:bg-black/60">
            <div
                onClick={(e) => e.stopPropagation()}
                aria-labelledby={titleId}
                role="dialog"
                aria-modal="true"
                className={`
                    text-small border border-border bg-card text-foreground
                    p-3 rounded-none md:rounded-card shadow-sm
                    overflow-y-auto p-5
                    ${className} ${ModalSizes[size]}`}
                        >
                    <div className="flex justify-between items-center">
                        <h2 id={titleId}
                            className="text-large">{title}</h2>

                        {/* <Button label="x"
                            onClick={onClose}
                            variant="cancel"
                            className="w-8 h-8
                                        flex justify-center items-center"
                        /> */}
                    </div>
                {children}
            </div>
        </div>
    )
}
