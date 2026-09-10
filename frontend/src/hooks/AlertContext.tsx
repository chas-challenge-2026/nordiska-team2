import { createContext } from "react";
import type { AlertType } from "../components/ui/Alert";

export type AlertItem = {
    id: string;
    type: AlertType;
    title?: string;
    message: string;
}

export type showAlertInput = Omit<AlertItem, "id"> & { duration?: number };

export type AlertContextValue = {
    showAlert: (alert: showAlertInput) => void;
};

export const AlertContext = createContext<AlertContextValue | null>(null)

