import LinkButton from "../../../components/ui/LinkButton";
import type { QuickAction } from "../data/quickActions";

type QuickActionsProps = {
    actions: QuickAction[];
};

export default function QuickActions({ actions }: QuickActionsProps){
    return ( 
        <div className="grid grid-cols-2 gap-2
                        sm:grid-cols-3
                        xl:grid-cols-6">
            {actions.map((action) =>(
                <LinkButton
                    key={action.id}
                    to={action.to}
                    size={action.size}
                    icon={
                        <img src={action.icon}
                            alt=""
                            aria-hidden="true"
                            className="size-7 sm:size-9"
                        />
                    }
                >
                    {action.label}
                </LinkButton>
            ))}
        </div>
    );
};
