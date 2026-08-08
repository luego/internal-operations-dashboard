import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import Home from "./page";

describe("Home", () => {
  it("presents the OpsDesk product entry point", () => {
    render(<Home />);

    expect(
      screen.getByRole("heading", { name: "Internal operations, under control." }),
    ).toBeInTheDocument();
    expect(screen.getByText("OpsDesk")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Open workspace" })).toHaveAttribute(
      "href",
      "/login",
    );
  });
});
