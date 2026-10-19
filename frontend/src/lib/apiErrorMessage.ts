import axios from "axios";
import type { TFunction } from "i18next";

// What the backend sends on errors. Every field is optional: older endpoints
// only send `error`, validation sends `errors`, the rate limiter sends nothing.
type ApiErrorBody = {
    code?: string;                       // stable code, e.g. "insufficient_funds"
    error?: string;                      // backend's text, already in the request's language
    params?: Record<string, unknown>;    // e.g. { max: 100 }
    errors?: Record<string, string[]>;   // ASP.NET validation: { Description: ["..."] }
};

// Turns any error from apiClient into one message for the user, in the
// selected language. Use it everywhere an error is shown, so the rules
// live in one place.
export function apiErrorMessage(err: unknown, t: TFunction): string {
    if (axios.isAxiosError<ApiErrorBody>(err)) {
        // No response at all: backend down, network gone, CORS.
        if (!err.response) return t("errors.network");

        // The rate limiter answers before any controller, so there's no body.
        if (err.response.status === 429) return t("errors.rate_limited");

        const data = err.response.data;

        // Known code: translate here. An unknown code (backend newer than
        // the frontend) falls back to the backend's own text.
        if (data?.code)
            return t(`errors.${data.code}`, { ...data.params, defaultValue: data.error ?? t("errors.unknown") });

        if (data?.error) return data.error;

        const firstValidationError = data?.errors && Object.values(data.errors)[0]?.[0];
        if (firstValidationError) return firstValidationError;
    }
    return t("errors.unknown");
}