/**
 * Read one-time OAuth ticket token from the URL.
 * Prefer query (survives HTTP 302). Hash is still accepted as a fallback.
 */
export function readOAuthTicketTokenFromLocation(
  search: string,
  hash: string
): string | null {
  const fromQuery = new URLSearchParams(
    search.startsWith("?") ? search.slice(1) : search
  ).get("token")
  if (fromQuery && fromQuery.trim() !== "") {
    return fromQuery.trim()
  }

  const fromHash = new URLSearchParams(
    hash.startsWith("#") ? hash.slice(1) : hash
  ).get("token")
  if (fromHash && fromHash.trim() !== "") {
    return fromHash.trim()
  }

  return null
}

/** Remove token from the address bar after it has been read (Referer / history). */
export function stripOAuthTicketTokenFromLocation(
  locationLike: Pick<Location, "pathname" | "search" | "hash"> = window.location,
  historyLike: Pick<History, "replaceState"> = window.history
): void {
  const params = new URLSearchParams(
    locationLike.search.startsWith("?")
      ? locationLike.search.slice(1)
      : locationLike.search
  )
  if (!params.has("token") && !locationLike.hash.includes("token=")) {
    return
  }

  params.delete("token")
  const nextSearch = params.toString()
  const nextUrl =
    locationLike.pathname + (nextSearch ? `?${nextSearch}` : "")
  historyLike.replaceState(null, "", nextUrl)
}

const LOGIN_TICKET_KEY = "tummly.oauth.loginTicket"
const SIGNUP_TICKET_KEY = "tummly.oauth.signupTicket"

/**
 * Capture ticket from the URL into sessionStorage (Strict Mode safe), then
 * strip it from the address bar. Re-reads the stash when the URL is empty.
 */
export function captureOAuthTicketToken(
  kind: "login" | "signup",
  search: string = typeof window !== "undefined" ? window.location.search : "",
  hash: string = typeof window !== "undefined" ? window.location.hash : "",
  storage: Pick<Storage, "getItem" | "setItem" | "removeItem"> = sessionStorage
): string {
  const key = kind === "login" ? LOGIN_TICKET_KEY : SIGNUP_TICKET_KEY
  const fromUrl = readOAuthTicketTokenFromLocation(search, hash)
  if (fromUrl) {
    storage.setItem(key, fromUrl)
    if (typeof window !== "undefined") {
      stripOAuthTicketTokenFromLocation()
    }
    return fromUrl
  }

  return storage.getItem(key)?.trim() ?? ""
}

export function clearOAuthTicketToken(
  kind: "login" | "signup",
  storage: Pick<Storage, "removeItem"> = sessionStorage
): void {
  storage.removeItem(kind === "login" ? LOGIN_TICKET_KEY : SIGNUP_TICKET_KEY)
}
