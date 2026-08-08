import { jwtVerify, type JWTPayload } from "jose";
import { z } from "zod";

export const ACCESS_COOKIE = "ops_access";
export const REFRESH_COOKIE = "ops_refresh";

const roleClaim = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

export type SessionUser = {
  id: string;
  displayName: string;
  roles: string[];
};

export type AuthEnvironment = {
  apiBaseUrl: string;
  signingKey: string;
  issuer: string;
  audience: string;
};

export type LoginInput = {
  identifier: string;
  password: string;
  deviceDescription?: string;
};

export const tokenPairSchema = z.object({
  accessToken: z.string().min(1),
  accessTokenExpiresAtUtc: z.iso.datetime({ offset: true }),
  refreshToken: z.string().min(1),
  refreshTokenExpiresAtUtc: z.iso.datetime({ offset: true }),
  tokenType: z.literal("Bearer"),
});

export type TokenPair = z.infer<typeof tokenPairSchema>;

type AuthenticationResult =
  | { ok: true; user: SessionUser; tokens: TokenPair }
  | { ok: false; status: number; error: string };

function rolesFrom(payload: JWTPayload): string[] {
  const value = payload[roleClaim] ?? payload.role ?? payload.roles;
  if (typeof value === "string") return [value];
  if (Array.isArray(value) && value.every((role) => typeof role === "string")) {
    return [...new Set(value)];
  }
  return [];
}

export function environment(): AuthEnvironment {
  const signingKey = process.env.AUTH_JWT_SIGNING_KEY;
  const issuer = process.env.AUTH_JWT_ISSUER;
  const audience = process.env.AUTH_JWT_AUDIENCE;
  if (!signingKey || !issuer || !audience) {
    throw new Error("Authentication JWT environment is not configured");
  }
  return {
    apiBaseUrl: (process.env.API_BASE_URL || "http://localhost:8080").replace(/\/$/, ""),
    signingKey,
    issuer,
    audience,
  };
}

export async function verifyAccessToken(
  token: string,
  config: AuthEnvironment,
): Promise<SessionUser> {
  const { payload } = await jwtVerify(token, new TextEncoder().encode(config.signingKey), {
    algorithms: ["HS256"],
    issuer: config.issuer,
    audience: config.audience,
  });
  const displayName = payload.unique_name ?? payload.name;
  const roles = rolesFrom(payload);
  if (!payload.sub || typeof displayName !== "string" || !displayName.trim() || roles.length === 0) {
    throw new Error("Access token has incomplete user claims");
  }
  return { id: payload.sub, displayName, roles };
}

function safeBackendFailure(status: number): AuthenticationResult {
  if ([400, 401, 415, 429].includes(status)) {
    return { ok: false, status, error: "Unable to sign in with those credentials." };
  }
  return { ok: false, status: 502, error: "Authentication service is unavailable." };
}

export async function authenticate(
  input: LoginInput,
  config: AuthEnvironment,
  fetcher: typeof fetch = fetch,
): Promise<AuthenticationResult> {
  let response: Response;
  try {
    response = await fetcher(`${config.apiBaseUrl}/api/v1/auth/login`, {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify(input),
      cache: "no-store",
    });
  } catch {
    return { ok: false, status: 503, error: "Authentication service is unavailable." };
  }
  if (!response.ok) return safeBackendFailure(response.status);

  try {
    const tokens = tokenPairSchema.parse(await response.json());
    const user = await verifyAccessToken(tokens.accessToken, config);
    return { ok: true, user, tokens };
  } catch {
    return { ok: false, status: 502, error: "Authentication service is unavailable." };
  }
}

export function secureCookies(runtime: Record<string, string | undefined> = process.env) {
  const configured = runtime.AUTH_COOKIE_SECURE?.toLowerCase();
  if (configured === "true") return true;
  if (configured === "false") return false;
  return runtime.NODE_ENV === "production";
}

export function cookieOptions(name: string, expires: Date, secure: boolean) {
  return {
    httpOnly: true as const,
    secure,
    sameSite: name === REFRESH_COOKIE ? ("strict" as const) : ("lax" as const),
    path: name === REFRESH_COOKIE ? "/api" : "/",
    expires,
  };
}

export async function logout(
  refreshToken: string | undefined,
  config: AuthEnvironment,
  fetcher: typeof fetch = fetch,
): Promise<void> {
  if (!refreshToken) return;
  try {
    await fetcher(`${config.apiBaseUrl}/api/v1/auth/logout`, {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify({ refreshToken }),
      cache: "no-store",
    });
  } catch {
    // Local logout must succeed even when the API cannot revoke the session.
  }
}
