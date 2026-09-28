import type { ReactNode } from "react";
import { X } from "lucide-react"; 

type Props = {
    isOpen: boolean; 
    onClose: () => void;
    title: string;
    children: ReactNode;
};

export function FaqCategoryModal({ isOpen, onClose, title, children }: Props) {
    if (!isOpen) return null;

    return (
        <div className="fixed inset-0 z-50 flex items-end md:items-center justify-center bg-black/50">
            <div className="bg-white w-full md:max-w-lg max-h-[90vh] overflow-y-auto rounded-2xl p-6">
                <div className="flex justify-between items-center mb-4">
                    <h2 className="text-lf font-semibold">{title}</h2>
                    <button onClick={onClose} aria-label="Stäng">
                        <X size={20} />
                            </button> 
                </div>
                {children}
            </div>
        </div>
    );
}