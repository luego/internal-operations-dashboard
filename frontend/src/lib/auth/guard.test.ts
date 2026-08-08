// @vitest-environment node

import { describe, expect, it } from "vitest";

import { getAuthenticatedUser } from "./guard";

const env = {
  apiBaseUrl: "http://api:8080",
  signingKey: "correct-horse-battery-staple-123456789",
  issuer: "ops-api",
  audience: "ops-web",
};

describe("getAuthenticatedUser", () => {
  it("returns null when the access cookie is missing or invalid", async () => {
    await expect(getAuthenticatedUser(undefined, env)).resolves.toBeNull();
    await expect(getAuthenticatedUser("not-a-jwt", env)).resolves.toBeNull();
  });
});
