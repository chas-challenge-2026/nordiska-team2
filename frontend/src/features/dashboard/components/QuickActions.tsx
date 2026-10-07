import React, { useState } from "react";
import Button from "../../../components/ui/Button";
import type { QuickAction } from "../../../types/quickActions";
import TransferModal from "./modals/TransferModal";
import TransactionModal from "./modals/TransactionModal";
// import PayBillsModal from "./modals/PayBillsModal";
// import LoanApplicationModal from "./modals/LoanApplicationModal";
// import AiBuddyModal from "./modals/AiBuddyModal";

type variantSizes = "xsmall" | "small" | "medium" | "large" ;

const variantSizesClasses: Record<NonNullable<variantSizes>, string> = {
    xsmall: "text-xsmall",
    small: "text-small",
    medium: "text-medium",
    large: "text-large",
}

type QuickActionsProps = {
    actions: QuickAction[];
};

const modalById: Record<string, React.ComponentType<{ isOpen: boolean; onClose: () => void }>> = {
    transaction: TransactionModal,
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
            <section aria-labelledby="quick-actions-title"
                     className="hidden lg:flex flex-col gap-3">
                <h2 id="quick-actions-title"
                    className="text-medium font-semibold text-foreground">
                    Snabbt &amp; enkelt
                </h2>

                <div className="grid grid-cols-2 sm:grid-cols-3 xl:grid-cols-5 gap-3">
                    {actions.map((action) =>(
                        <Button
                            variant="tile"
                            className={`group flex flex-col justify-center items-center gap-3
                                        h-full py-5 rounded-card!
                                        bg-linear-to-br! from-white/80 to-sky-100/70 backdrop-blur-md
                                        border-sky-200! shadow-md! shadow-brand/10!
                                        dark:from-card/80 dark:to-card/60 dark:border-border! dark:shadow-none!
                                        transition duration-300
                                        hover:-translate-y-1 hover:to-sky-200/80 hover:border-sky-300!
                                        hover:shadow-xl! hover:shadow-brand/25!
                                        dark:hover:bg-card!
                                        focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand
                                        ${variantSizesClasses[action.size ?? "small"]}`}
                            key={action.id}
                            label={action.label}
                            onClick={() => setActiveModal(action.id)}
                            icon={
                                // Ikonrutan fylls med Nordiska-blått vid hovring och ikonen blir vit.
                                <span className="grid place-items-center size-12 rounded-xl
                                                 bg-brand/10 transition duration-300
                                                 group-hover:bg-brand group-hover:scale-110 group-hover:-rotate-3">
                                    <img src={action.icon}
                                        alt=""
                                        aria-hidden="true"
                                        className="size-7 transition duration-300
                                                   dark:brightness-0 dark:invert dark:opacity-85
                                                   group-hover:brightness-0 group-hover:invert"
                                    />
                                </span>
                            }
                        />
                    ))}
                </div>
            </section>

            {ActiveModal && (
                <ActiveModal isOpen={true} onClose={() => setActiveModal(null)} />
            )}
       </>
    );
};
