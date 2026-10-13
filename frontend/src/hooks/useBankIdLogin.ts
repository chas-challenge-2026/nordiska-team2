import { useState, useRef, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import { apiClient } from "../client";
import { useAuth } from "./useAuth";

type BankIdStatus = "idle" | "pending" | "complete" | "failed";

const POLL_INTERVAL_MS = 1000;
const TIMEOUT_MS = 30000;

export function useBankIdLogin() {
  const [status, setStatus] = useState<BankIdStatus>("idle");
  // errorMessage är en nyckel i språkfilerna, t.ex. "login.errors.bankIdTimeout".
  // Den översätts med t() där den visas (LoginPage).
  const [errorMessage, setErrorMessage] = useState("");
  const auth = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  // Kept in refs so the cleanup below can always reach the CURRENT
  // timers, and so a poll that resolves after unmount doesn't try to
  // update state on a component that's gone.
  const timerRef = useRef<number | null>(null);
  const cancelledRef = useRef(false);

  function stopPolling() {
    if (timerRef.current !== null) {
      window.clearTimeout(timerRef.current);
      timerRef.current = null;
    }
  }

  // Runs on unmount (e.g. the user navigates away mid-login).
  useEffect(() => {
    return () => {
      cancelledRef.current = true;
      stopPolling();
    };
  }, []);

  async function start(personalId: string) {
    setErrorMessage("");
    setStatus("pending");
    cancelledRef.current = false;

    try {
      const response = await apiClient.post("/auth/bankid/start", { personalId });
      const orderRef = response.data.orderRef;
      const deadline = Date.now() + TIMEOUT_MS;
      poll(orderRef, deadline);
    } catch {
      setStatus("failed");
      setErrorMessage("login.errors.bankIdStartFailed");
    }
  }

  async function poll(orderRef: string, deadline: number) {
    if (cancelledRef.current) return;

    if (Date.now() > deadline) {
      setStatus("failed");
      setErrorMessage("login.errors.bankIdTimeout");
      return;
    }

    try {
      const response = await apiClient.get(`/auth/bankid/status/${orderRef}`);
      if (cancelledRef.current) return;

      if (response.data.status === "pending") {
        // setTimeout after each response, not setInterval.
        // Avoids stacking overlapping requests if one is slow.
        timerRef.current = window.setTimeout(() => poll(orderRef, deadline), POLL_INTERVAL_MS);
        return;
      }

      if (response.data.status === "complete") {
        // Identical to password login from here: the backend issued
        // a real token and set the same HttpOnly refresh cookie.
        setStatus("complete");
        auth.setAccessToken(response.data.accessToken);
        queryClient.clear(); // in case the previous user's cached data is stored
        navigate("/dashboard");
        return;
      }

      setStatus("failed");
      setErrorMessage("login.errors.bankIdFailed");
    } catch {
      if (cancelledRef.current) return;
      setStatus("failed");
      setErrorMessage("login.errors.bankIdError");
    }
  }

  function cancel() {
    cancelledRef.current = true;
    stopPolling();
    setStatus("idle");
  }

  return { status, errorMessage, start, cancel };
}