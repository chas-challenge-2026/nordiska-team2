import { render, screen } from "@testing-library/react";
import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import i18n from "../language";
import NotificationsCard from "../components/cards/NotificationsCard";
import NotificationToastListener from "../components/notifications/NotificationToastListener";

const { mockUseNotifications, mockShowAlert } = vi.hoisted(() => ({
  mockUseNotifications: vi.fn(),
  mockShowAlert: vi.fn(),
}));

vi.mock("../hooks/useNotifications", () => ({
  useNotifications: mockUseNotifications,
}));

vi.mock("../hooks/useAlert", () => ({
  useAlert: () => ({ showAlert: mockShowAlert }),
}));

describe("NotificationsCard", () => {
  beforeEach(async () => {
    mockUseNotifications.mockReset();
    mockShowAlert.mockReset();
    await i18n.changeLanguage("sv");
  });

  afterEach(async () => {
    await i18n.changeLanguage("sv");
  });

  it("visar laddningstext medan notiser hämtas", () => {
    mockUseNotifications.mockReturnValue({
      data: undefined,
      isLoading: true,
      isError: false,
    });

    render(<NotificationsCard />);
    expect(screen.getByText("Laddar...")).toBeInTheDocument();
  });

  it("visar tomtext när det inte finns några notiser", () => {
    mockUseNotifications.mockReturnValue({
      data: [],
      isLoading: false,
      isError: false,
    });

    render(<NotificationsCard />);
    expect(screen.getByText("Inga notiser ännu")).toBeInTheDocument();
  });

  it("visar felmeddelande när hämtningen misslyckas", () => {
    mockUseNotifications.mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: true,
    });

    render(<NotificationsCard />);
    expect(screen.getByText("Kunde inte hämta notiser")).toBeInTheDocument();
  });

  it("visar notiser på svenska och översätter hela meddelandet till engelska vid språkbyte", async () => {
    mockUseNotifications.mockReturnValue({
      data: [
        {
          id: 1,
          type: "deposit_confirmation",
          refId: "101",
          message: "Insättning på 2 500,00 kr till konto NKM-10001 har genomförts.",
          status: "sent",
          createdAt: "2026-10-15T10:00:00Z",
          sentAt: "2026-10-15T10:00:05Z",
          amount: 2500,
          accountNumber: "NKM-10001",
        },
        {
          id: 2,
          type: "withdrawal_confirmation",
          refId: "102",
          message: "Uttag på 400,00 kr från konto NKM-10001 har genomförts.",
          status: "pending",
          createdAt: "2026-10-15T11:00:00Z",
          sentAt: null,
          amount: 400,
          accountNumber: "NKM-10001",
        },
      ],
      isLoading: false,
      isError: false,
    });

    const { rerender } = render(<NotificationsCard />);

    expect(screen.getByText("Insättningsbekräftelse")).toBeInTheDocument();
    expect(
      screen.getByText(/Insättning på .* till konto NKM-10001 har genomförts\./)
    ).toBeInTheDocument();
    expect(screen.getByText("Skickad")).toBeInTheDocument();

    expect(screen.getByText("Uttagsbekräftelse")).toBeInTheDocument();
    expect(
      screen.getByText(/Uttag på .* från konto NKM-10001 har genomförts\./)
    ).toBeInTheDocument();
    expect(screen.getByText("Väntar")).toBeInTheDocument();

    // Switch language to English and verify 100% of the notification text translates
    await i18n.changeLanguage("en");
    rerender(<NotificationsCard />);

    expect(screen.getByText("Deposit confirmation")).toBeInTheDocument();
    expect(
      screen.getByText(/Deposit of .* to account NKM-10001 has been completed\./)
    ).toBeInTheDocument();
    expect(screen.getByText("Sent")).toBeInTheDocument();

    expect(screen.getByText("Withdrawal confirmation")).toBeInTheDocument();
    expect(
      screen.getByText(/Withdrawal of .* from account NKM-10001 has been completed\./)
    ).toBeInTheDocument();
    expect(screen.getByText("Pending")).toBeInTheDocument();
  });

  it("visar en lokaliserad toast-notis när en väntande notis övergår till skickad", async () => {
    await i18n.changeLanguage("en");

    mockUseNotifications.mockReturnValue({
      data: [
        {
          id: 10,
          type: "deposit_confirmation",
          refId: "201",
          message: "Insättning på 500,00 kr till konto NKM-10001 har genomförts.",
          status: "pending",
          createdAt: "2026-10-15T12:00:00Z",
          sentAt: null,
          amount: 500,
          accountNumber: "NKM-10001",
        },
      ],
      isLoading: false,
      isError: false,
    });

    const { rerender } = render(<NotificationToastListener />);
    expect(mockShowAlert).not.toHaveBeenCalled();

    mockUseNotifications.mockReturnValue({
      data: [
        {
          id: 10,
          type: "deposit_confirmation",
          refId: "201",
          message: "Insättning på 500,00 kr till konto NKM-10001 har genomförts.",
          status: "sent",
          createdAt: "2026-10-15T12:00:00Z",
          sentAt: "2026-10-15T12:00:01Z",
          amount: 500,
          accountNumber: "NKM-10001",
        },
      ],
      isLoading: false,
      isError: false,
    });

    rerender(<NotificationToastListener />);

    expect(mockShowAlert).toHaveBeenCalledWith(
      expect.objectContaining({
        type: "success",
        title: "Deposit confirmation",
        message: expect.stringMatching(/Deposit of .* to account NKM-10001 has been completed\./),
      })
    );
  });
});
