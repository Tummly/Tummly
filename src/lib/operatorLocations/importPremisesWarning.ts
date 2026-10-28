/** Import row warning from the API: the row is created as Draft and needs review. */
export const POSSIBLE_DUPLICATE_PREMISES_CODE = "possible_duplicate_premises"

export const POSSIBLE_DUPLICATE_PREMISES_MESSAGE =
  "Possible duplicate premises. Saved as Draft for review."

export function isPossibleDuplicatePremises(
  warning: { code: string } | null | undefined
): boolean {
  return warning?.code === POSSIBLE_DUPLICATE_PREMISES_CODE
}
