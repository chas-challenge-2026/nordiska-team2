import { describe, it, expect, beforeAll } from "vitest";
import { AxiosError, type AxiosResponse } from "axios";
import i18n from "../language";          // the same i18n module client.ts imports
import { apiErrorMessage } from "../lib/apiErrorMessage";

// An axios error with a response, the way apiClient rejects it.
function apiError(status: number, data?: unknown) {
    const response = { status, data, statusText: "", headers: {}, config: {} } as unknown as AxiosResponse;
    return new AxiosError("Request failed", undefined, undefined, undefined, response);
}

describe("apiErrorMessage", () => {
    const t = i18n.t.bind(i18n);
    beforeAll(() => i18n.changeLanguage("en"));

    it("translates a known code and fills in its params", () => {
        const err = apiError(400, { code: "description_too_long", error: "Beskrivningen får vara högst 100 tecken.", params: { max: 100 } });
        expect(apiErrorMessage(err, t)).toBe("The description can be at most 100 characters.");
    });

    it("falls back to the backend's text for a code the frontend doesn't know yet", () => {
        const err = apiError(400, { code: "brand_new_rule", error: "Backend text." });
        expect(apiErrorMessage(err, t)).toBe("Backend text.");
    });

    it("shows the backend's text when there is no code (today's backend)", () => {
        expect(apiErrorMessage(apiError(400, { error: "Otillräckligt saldo." }), t)).toBe("Otillräckligt saldo.");
    });

    it("shows the first validation error", () => {
        const err = apiError(400, { errors: { Description: ["Too long."] } });
        expect(apiErrorMessage(err, t)).toBe("Too long.");
    });

    it("has its own message for rate limiting", () => {
        expect(apiErrorMessage(apiError(429), t)).toBe("Too many attempts. Please wait a minute and try again.");
    });

    it("has its own message when the server can't be reached", () => {
        expect(apiErrorMessage(new AxiosError("Network Error", "ERR_NETWORK"), t)).toBe("Could not reach the server. Check your connection.");
    });

    it("never throws on something that isn't an axios error", () => {
        expect(apiErrorMessage(new Error("boom"), t)).toBe("Something went wrong. Please try again.");
    });
});