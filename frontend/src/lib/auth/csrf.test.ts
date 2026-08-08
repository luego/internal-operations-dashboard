import { describe, expect, it } from "vitest";

import { isSameOriginRequest } from "./csrf";

describe("BFF CSRF protection", () => {
  it("accepts an exact Origin match", () => {
    const request = new Request("https://ops.example.test/api/auth/logout", {
      method: "POST",
      headers: { origin: "https://ops.example.test" },
    });

    expect(isSameOriginRequest(request)).toBe(true);
  });

  it("rejects requests from a sibling origin", () => {
    const request = new Request("https://ops.example.test/api/backend/tickets", {
      method: "POST",
      headers: { origin: "https://compromised.example.test" },
    });

    expect(isSameOriginRequest(request)).toBe(false);
  });

  it("accepts a same-origin Referer when Origin is unavailable", () => {
    const request = new Request("https://ops.example.test/api/backend/tickets", {
      method: "PATCH",
      headers: { referer: "https://ops.example.test/tickets/42" },
    });

    expect(isSameOriginRequest(request)).toBe(true);
  });

  it("fails closed without Origin or Referer", () => {
    const request = new Request("https://ops.example.test/api/auth/logout", { method: "POST" });

    expect(isSameOriginRequest(request)).toBe(false);
  });
});
