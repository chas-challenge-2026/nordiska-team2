import { render, screen } from "@testing-library/react";
import { describe, it, expect } from "vitest";
import Alert from "../components/ui/Alert";

describe("Alert", () => {
  it("visar meddelandet", () => {
    render(<Alert type="info" message="Något gick fel" />);
    expect(screen.getByText("Något gick fel")).toBeInTheDocument();
  });

  it("ger rätt bakgrundsfärg-klass för type=error", () => {
    render(<Alert type="error" message="Fel" />);
    const alertBox = screen.getByText("Fel").parentElement;
    expect(alertBox).toHaveClass("bg-cancel");
  });

  it("är osynlig (opacity-0) när visible=false", () => {
    render(<Alert type="info" message="Dold" visible={false} />);
    const alertBox = screen.getByText("Dold").parentElement;
    expect(alertBox).toHaveClass("opacity-0");
  });

  it("är synlig (opacity-100) som standard", () => {
    render(<Alert type="info" message="Synlig" />);
    const alertBox = screen.getByText("Synlig").parentElement;
    expect(alertBox).toHaveClass("opacity-100");
  });
});
