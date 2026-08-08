// @vitest-environment node

import { SignJWT } from "jose";
import { describe, expect, it, vi } from "vitest";

import { proxyBackend } from "./backend-proxy";

const key = "correct-horse-battery-staple-123456789";
const env = {
  apiBaseUrl: "http://api:8080",
  signingKey: key,
  issuer: "ops-api",
  audience: "ops-web",
};

async function accessToken() {
  return new SignJWT({ unique_name: "Ada Agent", role: "Agent" })
    .setProtectedHeader({ alg: "HS256" })
    .setSubject("4b47ec2d-50f4-47fc-95cd-e745d4798ca6")
    .setIssuer(env.issuer)
    .setAudience(env.audience)
    .setIssuedAt()
    .setExpirationTime("15m")
    .sign(new TextEncoder().encode(key));
}

describe("backend proxy", () => {
  it("forwards the request to the versioned API with the bearer token", async () => {
    const fetcher = vi.fn<typeof fetch>().mockResolvedValue(Response.json({ totalTickets: 7 }));

    const result = await proxyBackend(
      {
        method: "GET",
        path: "dashboard/summary",
        query: "",
        accessToken: "access-secret",
      },
      env,
      fetcher,
    );

    expect(fetcher).toHaveBeenCalledWith("http://api:8080/api/v1/dashboard/summary", {
      method: "GET",
      headers: { authorization: "Bearer access-secret" },
      body: undefined,
      cache: "no-store",
    });
    expect(result.clearSession).toBe(false);
    await expect(result.response.json()).resolves.toEqual({ totalTickets: 7 });
  });

  it("rotates the refresh token and retries one time after an unauthorized response", async () => {
    const rotatedAccess = await accessToken();
    const fetcher = vi
      .fn<typeof fetch>()
      .mockResolvedValueOnce(new Response(null, { status: 401 }))
      .mockResolvedValueOnce(
        Response.json({
          accessToken: rotatedAccess,
          accessTokenExpiresAtUtc: "2026-08-08T12:15:00Z",
          refreshToken: "rotated-refresh",
          refreshTokenExpiresAtUtc: "2026-08-15T12:00:00Z",
          tokenType: "Bearer",
        }),
      )
      .mockResolvedValueOnce(Response.json({ totalTickets: 8 }));

    const result = await proxyBackend(
      {
        method: "GET",
        path: "dashboard/summary",
        query: "?days=30",
        accessToken: "expired-access",
        refreshToken: "current-refresh",
      },
      env,
      fetcher,
    );

    expect(fetcher).toHaveBeenNthCalledWith(2, "http://api:8080/api/v1/auth/refresh", {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify({ refreshToken: "current-refresh", deviceDescription: "OpsDesk BFF" }),
      cache: "no-store",
      signal: expect.any(AbortSignal),
    });
    expect(fetcher).toHaveBeenNthCalledWith(3, "http://api:8080/api/v1/dashboard/summary?days=30", {
      method: "GET",
      headers: { authorization: `Bearer ${rotatedAccess}` },
      body: undefined,
      cache: "no-store",
    });
    expect(result.tokens?.refreshToken).toBe("rotated-refresh");
    expect(result.clearSession).toBe(false);
  });

  it("deduplicates concurrent rotations for the same single-use refresh token", async () => {
    const rotatedAccess = await accessToken();
    let refreshCalls = 0;
    const fetcher = vi.fn<typeof fetch>(async (input, init) => {
      const url = String(input);
      const authorization = (init?.headers as Record<string, string> | undefined)?.authorization;
      if (url.endsWith("/auth/refresh")) {
        refreshCalls += 1;
        await new Promise((resolve) => setTimeout(resolve, 10));
        return Response.json({
          accessToken: rotatedAccess,
          accessTokenExpiresAtUtc: "2026-08-08T12:15:00Z",
          refreshToken: "concurrent-rotated-refresh",
          refreshTokenExpiresAtUtc: "2026-08-15T12:00:00Z",
          tokenType: "Bearer",
        });
      }
      if (authorization === `Bearer ${rotatedAccess}`) {
        return Response.json({ ok: true });
      }
      return new Response(null, { status: 401 });
    });

    const request = (path: string) =>
      proxyBackend(
        {
          method: "GET",
          path,
          query: "",
          accessToken: "expired-concurrent-access",
          refreshToken: "concurrent-single-use-refresh",
        },
        env,
        fetcher,
      );

    const [summary, trends] = await Promise.all([
      request("dashboard/summary"),
      request("dashboard/trends"),
    ]);

    expect(refreshCalls).toBe(1);
    expect(summary.response.status).toBe(200);
    expect(trends.response.status).toBe(200);
    expect(summary.tokens?.refreshToken).toBe("concurrent-rotated-refresh");
    expect(trends.tokens?.refreshToken).toBe("concurrent-rotated-refresh");
  });

  it("clears the local session when refresh is rejected", async () => {
    const fetcher = vi
      .fn<typeof fetch>()
      .mockResolvedValueOnce(new Response(null, { status: 401 }))
      .mockResolvedValueOnce(new Response(null, { status: 401 }));

    const result = await proxyBackend(
      {
        method: "GET",
        path: "tickets",
        query: "",
        accessToken: "expired-access",
        refreshToken: "replayed-refresh",
      },
      env,
      fetcher,
    );

    expect(result.response.status).toBe(401);
    expect(result.clearSession).toBe(true);
    expect(fetcher).toHaveBeenCalledTimes(2);
  });
});
