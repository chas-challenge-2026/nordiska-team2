import React, { useState } from "react";
import Button from "../../../components/ui/Button";
import type { QuickAction } from "../../../types/quickActions";
import TransferModal from "./modals/TransferModal";
// import PayBillsModal from "./modals/PayBillsModal";
// import LoanApplicationModal from "./modals/LoanApplicationModal";
// import AiBuddyModal from "./modals/AiBuddyModal";

type QuickActionsProps = {
    actions: QuickAction[];
};

const modalById: Record<string, React.ComponentType<{ isOpen: boolean; onClose: () => void }>> = {
    transfer: TransferModal,
    // payments: PayBillsModal,
    // loanApplication: LoanApplicationModal,
    // aiBuddy: AiBuddyModal,
};

export default function QuickActions({ actions }: QuickActionsProps){
    const [activeModal, setActiveModal] = useState<string | null>(null)
    const ActiveModal = activeModal ? modalById[activeModal] : null;

    return ( 
       <>
            <div className="grid grid-cols-2
                            sm:grid-cols-3
                            xl:grid-cols-6">
                {actions.map((action) =>(
                    <Button
                    className="flex flex-col justify-center items-center"
                        key={action.id}
                        label={action.label}
                        onClick={() => setActiveModal(action.id)}
                        icon={
                            <img src={action.icon}
                                alt=""
                                aria-hidden="true"
                                className="size-7 sm:size-9"
                            />
                        }
                    >
                    </Button>
                ))}
            </div>
            {ActiveModal && (
                <ActiveModal isOpen={true} onClose={() => setActiveModal(null)} />
            )}
         </>
    );
};
