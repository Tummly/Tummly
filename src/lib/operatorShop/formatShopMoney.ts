export function formatShopGbpFromPence(pence: number): string {
  return `£${(pence / 100).toFixed(2)}`
}

export function formatShopDisplayDate(iso: string): string {
  return new Date(iso).toLocaleDateString("en-GB", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  })
}

export function formatShopProgressTimestamp(
  iso: string | null | undefined
): string | null {
  if (iso == null) return null
  return new Date(iso).toLocaleString("en-GB", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  })
}
