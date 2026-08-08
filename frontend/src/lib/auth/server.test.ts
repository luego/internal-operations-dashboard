// @vitest-environment node

import { SignJWT } from "jose";
import { describe, expect, it, vi } from "vitest";

import {
  ACCESS_COOKIE,
  REFRESH_COOKIE,
  authenticate,
  cookieOptions,
  logout,
  secureCookies,
  verifyAccessToken,
} from "./server";

const key = "correct-horse-battery-staple-123456789";
const env = {
  apiBaseUrl: "http://api:8080",
  signingKey: key,
  issuer: "ops-api",
  audience: "ops-web",
};

async function token(overrides: Record<string, unknown> = {}) {
  return new SignJWT({
    unique_name: "Ada Agent",
    "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": ["Agent", "Viewer"],
    ...overrides,
  })
    .setProtectedHeader({ alg: "HS256" })
    .setSubject("4b47ec2d-50f4-47fc-95cd-e745d4798ca6")
    .setIssuer(env.issuer)
    .setAudience(env.audience)
    .setIssuedAt()
    .setExpirationTime("15m")
    .sign(new TextEncoder().encode(key));
}

describe("server authentication", () => {
  it("verifies the access JWT and exposes only safe user claims", async () => {
    const accessToken = await token();

    await expect(verifyAccessToken(accessToken, env)).resolves.toEqual({
      id: "4b47ec2d-50f4-47fc-95cd-e745d4798ca6",
      displayName: "Ada Agent",
      roles: ["Agent", "Viewer"],
    });
  });

  it("rejects tokens with an invalid audience or incomplete user claims", async () => {
    const wrongAudience = await new SignJWT({ unique_name: "Ada", role: "Agent" })
      .setProtectedHeader({ alg: "HS256" })
      .setSubject("user-id")
      .setIssuer(env.issuer)
      .setAudience("another-app")
      .setExpirationTime("15m")
      .sign(new TextEncoder().encode(key));

    await expect(verifyAccessToken(wrongAudience, env)).rejects.toThrow();
    await expect(verifyAccessToken(await token({ unique_name: "" }), env)).rejects.toThrow();
  });

  it("forwards login JSON and validates a complete token pair", async () => {
    const accessToken = await token();
    const fetcher = vi.fn<typeof fetch>().mockResolvedValue(
      Response.json({
        accessToken,
        accessTokenExpiresAtUtc: "2026-08-08T12:15:00Z",
        refreshToken: "refresh-secret",
        refreshTokenExpiresAtUtc: "2026-08-15T12:00:00Z",
        tokenType: "Bearer",
      }),
    );

    const result = await authenticate(
      { identifier: "ada@example.test", password: "secret" },
      env,
      fetcher,
    );

    expect(fetcher).toHaveBeenCalledWith("http://api:8080/api/v1/auth/login", {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify({ identifier: "ada@example.test", password: "secret" }),
      cache: "no-store",
    });
    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.user.displayName).toBe("Ada Agent");
      expect(result.tokens.accessToken).toBe(accessToken);
    }
  });

  it("returns safe failures and never accepts malformed backend success data", async () => {
    const denied = vi.fn<typeof fetch>().mockResolvedValue(
      Response.json({ detail: "account ada@example.test is locked" }, { status: 401 }),
    );
    const malformed = vi.fn<typeof fetch>().mockResolvedValue(
      Response.json({ accessToken: "leaked-token" }),
    );

    await expect(authenticate({ identifier: "ada", password: "bad" }, env, denied)).resolves.toEqual({
      ok: false,
      status: 401,
      error: "Unable to sign in with those credentials.",
    });
    await expect(authenticate({ identifier: "ada", password: "bad" }, env, malformed)).resolves.toEqual({
      ok: false,
      status: 502,
      error: "Authentication service is unavailable.",
    });
  });

  it("uses hardened cookie settings with refresh restricted to auth handlers", () => {
    expect(cookieOptions(ACCESS_COOKIE, new Date("2026-08-08T12:15:00Z"), false)).toMatchObject({
      httpOnly: true,
      secure: false,
      sameSite: "lax",
      path: "/",
    });
    expect(cookieOptions(REFRESH_COOKIE, new Date("2026-08-15T12:00:00Z"), true)).toMatchObject({
      httpOnly: true,
      secure: true,
      sameSite: "strict",
      path: "/api",
    });
  });

  it("allows local production containers to explicitly disable secure cookies", () => {
    expect(secureCookies({ NODE_ENV: "production" })).toBe(true);
    expect(secureCookies({ NODE_ENV: "production", AUTH_COOKIE_SECURE: "false" })).toBe(false);
    expect(secureCookies({ NODE_ENV: "development", AUTH_COOKIE_SECURE: "true" })).toBe(true);
  });

  it("calls backend logout when a refresh token exists and tolerates backend failure", async () => {
    const fetcher = vi.fn<typeof fetch>().mockRejectedValue(new Error("offline"));

    await expect(logout("refresh-secret", env, fetcher)).resolves.toBeUndefined();
    expect(fetcher).toHaveBeenCalledWith("http://api:8080/api/v1/auth/logout", {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify({ refreshToken: "refresh-secret" }),
      cache: "no-store",
    });
  });
});
