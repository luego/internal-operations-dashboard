import { createHash } from "node:crypto";

import type { AuthEnvironment, TokenPair } from "./server";
import { tokenPairSchema, verifyAccessToken } from "./server";

export type BackendProxyInput = {
  method: string;
  path: string;
  query: string;
  accessToken?: string;
  refreshToken?: string;
  body?: BodyInit;
  contentType?: string;
};

export type BackendProxyResult = {
  response: Response;
  tokens?: TokenPair;
  clearSession: boolean;
};

function requestHeaders(accessToken: string | undefined, contentType?: string) {
  const headers: Record<string, string> = {};
  if (accessToken) headers.authorization = `Bearer ${accessToken}`;
  if (contentType) headers["content-type"] = contentType;
  return headers;
}

async function send(
  input: BackendProxyInput,
  accessToken: string | undefined,
  config: AuthEnvironment,
  fetcher: typeof fetch,
) {
  return fetcher(`${config.apiBaseUrl}/api/v1/${input.path}${input.query}`, {
    method: input.method,
    headers: requestHeaders(accessToken, input.contentType),
    body: input.body,
    cache: "no-store",
  });
}

type Rotation = {
  settledAt?: number;
  promise: Promise<TokenPair | null>;
};

const rotationCache = new Map<string, Rotation>();
const rotationCacheLifetimeMs = 5_000;

function rotationKey(refreshToken: string, apiBaseUrl: string) {
  return createHash("sha256").update(apiBaseUrl).update("\0").update(refreshToken).digest("hex");
}

async function rotate(
  refreshToken: string,
  config: AuthEnvironment,
  fetcher: typeof fetch,
): Promise<TokenPair | null> {
  let response: Response;
  try {
    response = await fetcher(`${config.apiBaseUrl}/api/v1/auth/refresh`, {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify({ refreshToken, deviceDescription: "OpsDesk BFF" }),
      cache: "no-store",
      signal: AbortSignal.timeout(10_000),
    });
  } catch {
    return null;
  }
  if (!response.ok) return null;

  try {
    const tokens = tokenPairSchema.parse(await response.json());
    await verifyAccessToken(tokens.accessToken, config);
    return tokens;
  } catch {
    return null;
  }
}

function rotateOnce(refreshToken: string, config: AuthEnvironment, fetcher: typeof fetch) {
  const key = rotationKey(refreshToken, config.apiBaseUrl);
  const now = Date.now();
  const existing = rotationCache.get(key);
  if (existing && (existing.settledAt === undefined || existing.settledAt + rotationCacheLifetimeMs > now)) {
    return existing.promise;
  }

  const promise = rotate(refreshToken, config, fetcher);
  const rotation: Rotation = { promise };
  rotationCache.set(key, rotation);

  const scheduleCleanup = () => {
    rotation.settledAt = Date.now();
    const cleanup = setTimeout(() => {
      if (rotationCache.get(key) === rotation) rotationCache.delete(key);
    }, rotationCacheLifetimeMs);
    cleanup.unref();
  };
  void promise.then(scheduleCleanup, scheduleCleanup);
  return promise;
}

export async function proxyBackend(
  input: BackendProxyInput,
  config: AuthEnvironment,
  fetcher: typeof fetch = fetch,
): Promise<BackendProxyResult> {
  let response: Response;
  try {
    response = await send(input, input.accessToken, config, fetcher);
  } catch {
    return {
      response: Response.json({ error: "The operations service is unavailable." }, { status: 503 }),
      clearSession: false,
    };
  }

  if (response.status !== 401 || !input.refreshToken) {
    return {
      response,
      clearSession: response.status === 401,
    };
  }

  const tokens = await rotateOnce(input.refreshToken, config, fetcher);
  if (!tokens) {
    return { response: new Response(null, { status: 401 }), clearSession: true };
  }

  try {
    const retried = await send(input, tokens.accessToken, config, fetcher);
    return {
      response: retried,
      tokens,
      clearSession: retried.status === 401,
    };
  } catch {
    return {
      response: Response.json({ error: "The operations service is unavailable." }, { status: 503 }),
      tokens,
      clearSession: false,
    };
  }
}
