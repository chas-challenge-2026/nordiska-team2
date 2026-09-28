import { useState } from "react"
import { Plus, Minus} from "lucide-react"
import type { FaqEntry } from "../../types/faq"

type Props = {
    entry: FaqEntry;
    defaultOpen?: boolean;
};

export function FaqQuestionCard({ entry, defaultOpen = false}: Props) {
    const [isOpen, setIsOpen] = useState(defaultOpen);

    return (
        <div className="border border-gray-200 rounded-xl mb-3 bg-white">
            <button onClick={() => setIsOpen(!isOpen)} className="w-full flex justify-between items-center p-4 text-left font-semibold"
                >
                    {entry.question}
                    {isOpen ? <Minus size={18} /> : <Plus size={18} />}
            </button>

            {isOpen && (
                <div className="px-4 pb-4 text-gray-600">
                    {entry.answer}
                    </div>
            )}
        </div>
    )
}