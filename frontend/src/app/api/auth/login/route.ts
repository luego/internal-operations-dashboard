import { NextResponse } from "next/server";
import { z } from "zod";

import { isSameOriginRequest } from "@/lib/auth/csrf";
import {
  ACCESS_COOKIE,
  authenticate,
  cookieOptions,
  environment,
  REFRESH_COOKIE,
  secureCookies,
} from "@/lib/auth/server";

const loginSchema = z.object({
  identifier: z.string().min(1).max(320),
  password: z.string().min(1).max(1024),
  deviceDescription: z.string().max(200).optional(),
});

export async function POST(request: Request) {
  if (!isSameOriginRequest(request)) {
    return NextResponse.json({ error: "Request origin is not allowed." }, { status: 403 });
  }

  let input: z.infer<typeof loginSchema>;
  try {
    input = loginSchema.parse(await request.json());
  } catch {
    return NextResponse.json({ error: "Unable to sign in with those credentials." }, { status: 400 });
  }

  let result: Awaited<ReturnType<typeof authenticate>>;
  try {
    result = await authenticate(input, environment());
  } catch {
    return NextResponse.json({ error: "Authentication service is unavailable." }, { status: 503 });
  }
  if (!result.ok) {
    return NextResponse.json({ error: result.error }, { status: result.status });
  }

  const response = NextResponse.json({ user: result.user });
  const secure = secureCookies();
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
  return response;
}
