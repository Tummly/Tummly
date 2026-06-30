import { ukPostcodeRegex } from "@/lib/locationUpload/locationUploadValidation"

export const ADDRESS_SUGGEST_MIN_CHARS = 4
export const ADDRESS_SUGGEST_DEBOUNCE_MS = 500
export const ADDRESS_POSTCODE_MISMATCH_WARNING =
  "Selected Address doesn't match with postcode"
export const ADDRESS_USE_MY_ADDRESS_LABEL = "Use my address instead"
export const ADDRESS_MULTIPLE_PREMISES_NOTE =
  "Multiple addresses found for this postcode — we've picked the closest match. Use my address instead if this isn't right."
export const ADDRESS_RECONCILED_NOTE =
  "Address updated to match postcode"

export type AddressSuggestion = {
  id: string
  label: string
  address: string
  postcode: string
}

export type AddressResolveResult = {
  postcode: string
  address: string
  premises: Array<{ address: string; postcode: string }>
  multiplePremises: boolean
  usedBestMatch: boolean
}

export type VerifiedAddressPostcodePair = {
  address: string
  postcode: string
}

export function addressPostcodePairsMatch(
  verified: VerifiedAddressPostcodePair,
  address: string,
  postcode: string
) {
  return (
    postcodesMatch(verified.postcode, postcode) &&
    normalizeAddressForComparison(verified.address) ===
      normalizeAddressForComparison(address)
  )
}

export function normalizeAddressForComparison(value: string) {
  return value
    .toLowerCase()
    .replace(/[^a-z0-9\s]/g, "")
    .replace(/\s+/g, " ")
    .trim()
}

export function streetLinesOverlap(left: string, right: string) {
  const normalizedLeft = normalizeAddressForComparison(left)
  const normalizedRight = normalizeAddressForComparison(right)

  if (!normalizedLeft || !normalizedRight) {
    return false
  }

  return (
    normalizedLeft.includes(normalizedRight) ||
    normalizedRight.includes(normalizedLeft)
  )
}

export function postcodesMatch(left: string, right: string) {
  return (
    normalizeAddressForComparison(left.replace(/\s+/g, "")) ===
    normalizeAddressForComparison(right.replace(/\s+/g, ""))
  )
}

export function isValidUkPostcode(value: string) {
  return ukPostcodeRegex.test(value.trim())
}

export function pickBestAddressMatch(
  candidates: string[],
  addressHint: string
) {
  if (candidates.length === 0) {
    return null
  }

  if (!addressHint.trim()) {
    return candidates[0] ?? null
  }

  const normalizedHint = normalizeAddressForComparison(addressHint)
  let bestCandidate = candidates[0]
  let bestScore = -1

  for (const candidate of candidates) {
    const normalizedCandidate = normalizeAddressForComparison(candidate)
    let score = 0

    if (normalizedCandidate === normalizedHint) {
      score = 100
    } else if (
      normalizedCandidate.includes(normalizedHint) ||
      normalizedHint.includes(normalizedCandidate)
    ) {
      score = 50
    } else {
      const candidateTokens = new Set(normalizedCandidate.split(" "))
      score = normalizedHint
        .split(" ")
        .filter((token) => candidateTokens.has(token)).length
    }

    if (score > bestScore) {
      bestScore = score
      bestCandidate = candidate
    }
  }

  return bestScore > 0 ? bestCandidate : candidates[0]
}

/** Last comma-separated segment when lookup does not return post_town. */
export function extractTownCityFromAddress(address: string) {
  const parts = address
    .split(",")
    .map((part) => part.trim())
    .filter(Boolean)

  if (parts.length < 2) {
    return ""
  }

  return parts[parts.length - 1] ?? ""
}

export function shouldReconcileAddress(
  currentAddress: string,
  resolvedAddress: string,
  resolvedPostcode: string,
  enteredPostcode: string
) {
  if (!postcodesMatch(resolvedPostcode, enteredPostcode)) {
    return true
  }

  return !streetLinesOverlap(currentAddress, resolvedAddress)
}

export function shouldDeferPostcodeBlurLookup(input: {
  isResolvingSuggestion: boolean
  isResolvingPostcode: boolean
}) {
  return input.isResolvingSuggestion || input.isResolvingPostcode
}

export function isDuplicatePostcodeBlurSnapshot(
  snapshot: VerifiedAddressPostcodePair | null,
  address: string,
  postcode: string
) {
  if (!snapshot) {
    return false
  }

  return addressPostcodePairsMatch(snapshot, address, postcode)
}
