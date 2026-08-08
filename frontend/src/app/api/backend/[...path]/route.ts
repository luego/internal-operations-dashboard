import { cookies } from "next/headers";
import { NextResponse } from "next/server";

import { proxyBackend } from "@/lib/auth/backend-proxy";
import { isSameOriginRequest } from "@/lib/auth/csrf";
import {
  ACCESS_COOKIE,
  cookieOptions,
  environment,
  REFRESH_COOKIE,
  secureCookies,
} from "@/lib/auth/server";

const allowedResources = new Set(["dashboard", "departments", "tickets", "users"]);
const allowedSegment = /^[a-zA-Z0-9-]+$/;
const forwardedResponseHeaders = ["content-type", "location", "retry-after"];

type HandlerContext = {
  params: Promise<{ path: string[] }>;
};

async function handler(request: Request, context: HandlerContext) {
  const method = request.method.toUpperCase();
  if (method !== "GET" && method !== "HEAD" && !isSameOriginRequest(request)) {
    return NextResponse.json({ error: "Request origin is not allowed." }, { status: 403 });
  }

  const { path } = await context.params;
  if (
    path.length === 0 ||
    !allowedResources.has(path[0]) ||
    path.some((segment) => !allowedSegment.test(segment))
  ) {
    return NextResponse.json({ error: "Resource not found." }, { status: 404 });
  }

  const cookieStore = await cookies();
  const hasBody = method !== "GET" && method !== "HEAD";
  const result = await proxyBackend(
    {
      method,
      path: path.join("/"),
      query: new URL(request.url).search,
      accessToken: cookieStore.get(ACCESS_COOKIE)?.value,
      refreshToken: cookieStore.get(REFRESH_COOKIE)?.value,
      body: hasBody ? await request.arrayBuffer() : undefined,
      contentType: hasBody ? request.headers.get("content-type") ?? undefined : undefined,
    },
    environment(),
  );

  const headers = new Headers();
  for (const name of forwardedResponseHeaders) {
    const value = result.response.headers.get(name);
    if (value) headers.set(name, value);
  }
  const response = new NextResponse(result.response.body, {
    status: result.response.status,
    headers,
  });
  const secure = secureCookies();

  if (result.tokens) {
    response.cookies.set(
      ACCESS_COOKIE,
      result.tokens.accessToken,
      cookieOptions(ACCESS_COOKIE, new Date(result.tokens.accessTokenExpiresAtUtc), secure),
    );
    response.cookies.set(
      REFRESH_COOKIE,
      result.tokens.refreshToken,
      cookieOptions(REFRESH_COOKIE, new Date(result.tokens.refreshTokenExpiresAtUtc), secure),
    );
  }

  if (result.clearSession) {
    response.cookies.set(ACCESS_COOKIE, "", {
      ...cookieOptions(ACCESS_COOKIE, new Date(0), secure),
      maxAge: 0,
    });
    response.cookies.set(REFRESH_COOKIE, "", {
      ...cookieOptions(REFRESH_COOKIE, new Date(0), secure),
      maxAge: 0,
    });
  }

  return response;
}

export const GET = handler;
export const POST = handler;
export const PUT = handler;
export const PATCH = handler;
export const DELETE = handler;
