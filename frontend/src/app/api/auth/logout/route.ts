import { NextResponse } from "next/server";
import { cookies } from "next/headers";

import { isSameOriginRequest } from "@/lib/auth/csrf";
import {
  ACCESS_COOKIE,
  cookieOptions,
  environment,
  logout,
  REFRESH_COOKIE,
  secureCookies,
} from "@/lib/auth/server";

export async function POST(request: Request) {
  if (!isSameOriginRequest(request)) {
    return NextResponse.json({ error: "Request origin is not allowed." }, { status: 403 });
  }

  const refreshToken = (await cookies()).get(REFRESH_COOKIE)?.value;

  try {
    await logout(refreshToken, environment());
  } catch {
    // Clearing browser credentials is unconditional, including bad server configuration.
  }

  const response = new NextResponse(null, { status: 204 });
  const secure = secureCookies();
  response.cookies.set(ACCESS_COOKIE, "", {
    ...cookieOptions(ACCESS_COOKIE, new Date(0), secure),
    maxAge: 0,
  });
  response.cookies.set(REFRESH_COOKIE, "", {
    ...cookieOptions(REFRESH_COOKIE, new Date(0), secure),
    maxAge: 0,
  });
  return response;
}
