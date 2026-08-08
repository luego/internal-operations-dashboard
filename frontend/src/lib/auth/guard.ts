import type { AuthEnvironment, SessionUser } from "./server";
import { verifyAccessToken } from "./server";

export async function getAuthenticatedUser(
  accessToken: string | undefined,
  config: AuthEnvironment,
): Promise<SessionUser | null> {
  if (!accessToken) return null;
  try {
    return await verifyAccessToken(accessToken, config);
  } catch {
    return null;
  }
}
