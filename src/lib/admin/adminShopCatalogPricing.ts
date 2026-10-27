/** Catalog prices are stored in pence; the Admin editor edits pounds. */
export function penceToPoundsInput(pence: number): string {
  const whole = Math.floor(pence / 100)
  const remainder = pence % 100
  return `${whole}.${String(remainder).padStart(2, "0")}`
}

const POUNDS_PATTERN = /^(\d+)?(?:\.(\d{1,2}))?$/

/**
 * Parses a pound amount typed in the editor into whole pence.
 * Returns null when the text is empty, negative, or has more than two decimals.
 */
export function poundsInputToPence(input: string): number | null {
  const text = input.trim().replace(/^£/, "").trim()
  const match = POUNDS_PATTERN.exec(text)
  if (!match || (match[1] === undefined && match[2] === undefined)) {
    return null
  }
  const pounds = Number(match[1] ?? "0")
  const pence = Number((match[2] ?? "").padEnd(2, "0"))
  return pounds * 100 + pence
}
