import { NextResponse } from "next/server";
import { cookies } from "next/headers";

import { ACCESS_COOKIE, environment, verifyAccessToken } from "@/lib/auth/server";

export async function GET() {
  const accessToken = (await cookies()).get(ACCESS_COOKIE)?.value;

  if (!accessToken) {
    return NextResponse.json({ error: "Unauthorized" }, { status: 401 });
  }
  try {
    const user = await verifyAccessToken(accessToken, environment());
    return NextResponse.json({ user });
  } catch {
    return NextResponse.json({ error: "Unauthorized" }, { status: 401 });
  }
}
