function originOf(value: string | null) {
  if (!value) return null;
  try {
    return new URL(value).origin;
  } catch {
    return null;
  }
}

export function isSameOriginRequest(request: Request) {
  const expected = new URL(request.url).origin;
  const supplied = originOf(request.headers.get("origin")) ?? originOf(request.headers.get("referer"));
  return supplied === expected;
}
