import { render, screen, fireEvent } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import "../language";
import BankIdPinModal from "../components/DemoBankIdPinModal";

describe("DemoBankIdPinModal", () => {
  it("tar bara emot siffror i pin-fältet", () => {
    render(<BankIdPinModal onConfirm={vi.fn()} onCancel={vi.fn()} />);
    const input = screen.getByPlaceholderText("••••••") as HTMLInputElement;

    fireEvent.change(input, { target: { value: "a1b2c3" } });

    expect(input.value).toBe("123");
  });

  it("inaktiverar submit-knappen tills 6 siffror är ifyllda", () => {
    render(<BankIdPinModal onConfirm={vi.fn()} onCancel={vi.fn()} />);
    const input = screen.getByPlaceholderText("••••••");
    const submitButton = screen.getByRole("button", { name: "Identifiera" });

    expect(submitButton).toBeDisabled();

    fireEvent.change(input, { target: { value: "123456" } });

    expect(submitButton).not.toBeDisabled();
  });

  it("anropar onConfirm när formuläret skickas med 6 siffror", () => {
    const onConfirm = vi.fn();
    render(<BankIdPinModal onConfirm={onConfirm} onCancel={vi.fn()} />);

    fireEvent.change(screen.getByPlaceholderText("••••••"), { target: { value: "123456" } });
    fireEvent.click(screen.getByRole("button", { name: "Identifiera" }));

    expect(onConfirm).toHaveBeenCalledTimes(1);
  });

  it("anropar onCancel när man klickar på bakgrunden", () => {
    const onCancel = vi.fn();
    const { container } = render(<BankIdPinModal onConfirm={vi.fn()} onCancel={onCancel} />);

    fireEvent.click(container.firstChild as HTMLElement);

    expect(onCancel).toHaveBeenCalledTimes(1);
  });
});
