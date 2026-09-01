import axiosInstance from "@/api/axiosInstance"
import type {
  AddressResolveResult,
  AddressSuggestion,
} from "@/lib/addressLookup"
import { readBoolean, readString, unwrapDataObject } from "@/lib/apiEnvelope"
import { isAxiosError } from "axios"

const suggestSessionCache = new Map<string, AddressSuggestion[]>()
const suggestionResolveSessionCache = new Map<
  string,
  { address: string; postTown: string; postcode: string }
>()
const postcodeResolveSessionCache = new Map<string, AddressResolveResult>()

function normalizeSuggestQuery(query: string) {
  return query.trim().toLowerCase()
}

function normalizePostcodeResolveKey(postcode: string, addressHint?: string) {
  const normalizedPostcode = postcode.trim().replace(/\s+/g, "").toLowerCase()
  const normalizedHint = addressHint?.trim().toLowerCase() ?? ""

  return normalizedHint
    ? `${normalizedPostcode}|${normalizedHint}`
    : normalizedPostcode
}

export function isAddressLookupAbortError(error: unknown) {
  return (
    (error instanceof DOMException && error.name === "AbortError") ||
    (isAxiosError(error) && error.code === "ERR_CANCELED")
  )
}

function parseSuggestion(value: unknown): AddressSuggestion | null {
  const record = unwrapDataObject(value)

  if (!record) {
    return null
  }

  const id = readString(record, "id")
  const label = readString(record, "label")
  const address = readString(record, "address")
  const postcode = readString(record, "postcode")

  if (!id || !label) {
    return null
  }

  return {
    id,
    label,
    address: address ?? "",
    postcode: postcode ?? "",
  }
}

function parseResolveResult(value: unknown): AddressResolveResult | null {
  const record = unwrapDataObject(value)

  if (!record) {
    return null
  }

  const postcode = readString(record, "postcode")
  const address = readString(record, "address")

  if (!postcode || !address) {
    return null
  }

  const premisesRaw = record.premises

  const premises = Array.isArray(premisesRaw)
    ? premisesRaw
        .map((entry) => {
          const premise = unwrapDataObject(entry)

          if (!premise) {
            return null
          }

          const premiseAddress = readString(premise, "address")
          const premisePostcode = readString(premise, "postcode")
          const premisePostTown =
            readString(premise, "postTown") ?? readString(premise, "post_town")

          if (!premiseAddress) {
            return null
          }

          return {
            address: premiseAddress,
            postcode: premisePostcode ?? postcode,
            postTown: premisePostTown ?? "",
          }
        })
        .filter(
          (
            entry
          ): entry is {
            address: string
            postcode: string
            postTown: string
          } => Boolean(entry)
        )
    : []

  const postTown =
    readString(record, "postTown")
    ?? readString(record, "post_town")
    ?? premises.find((premise) => premise.postTown.trim())?.postTown
    ?? ""

  return {
    postcode,
    address,
    postTown,
    premises,
    multiplePremises: readBoolean(record, "multiplePremises") ?? premises.length > 1,
    usedBestMatch: readBoolean(record, "usedBestMatch") ?? false,
  }
}

function parseSuggestionPremise(value: unknown) {
  const record = unwrapDataObject(value)

  if (!record) {
    return null
  }

  const address = readString(record, "address")
  const postTown =
    readString(record, "postTown") ?? readString(record, "post_town")
  const postcode = readString(record, "postcode")

  if (!address) {
    return null
  }

  return {
    address,
    postTown: postTown ?? "",
    postcode: postcode ?? "",
  }
}

export async function suggestAddresses(
  query: string,
  signal?: AbortSignal
) {
  const normalizedQuery = normalizeSuggestQuery(query)

  if (normalizedQuery.length < 4) {
    return []
  }

  const cached = suggestSessionCache.get(normalizedQuery)

  if (cached) {
    return cached
  }

  const response = await axiosInstance.get("/address/suggest", {
    params: { q: query.trim() },
    skipAuthRedirect: true,
    signal,
  })

  const payload = response.data as { suggestions?: unknown[] }
  const suggestions = Array.isArray(payload.suggestions)
    ? payload.suggestions
        .map(parseSuggestion)
        .filter((entry): entry is AddressSuggestion => Boolean(entry))
    : []

  suggestSessionCache.set(normalizedQuery, suggestions)

  return suggestions
}

export async function resolveSuggestionAddress(suggestionId: string) {
  const normalizedId = suggestionId.trim()

  if (!normalizedId) {
    return null
  }

  const cached = suggestionResolveSessionCache.get(normalizedId)

  if (cached) {
    return cached
  }

  const response = await axiosInstance.get("/address/resolve-suggestion", {
    params: { id: normalizedId },
    skipAuthRedirect: true,
  })

  const resolved = parseSuggestionPremise(response.data)

  if (resolved) {
    suggestionResolveSessionCache.set(normalizedId, resolved)
  }

  return resolved
}

export async function resolvePostcodeAddress(
  postcode: string,
  addressHint?: string
) {
  const cacheKey = normalizePostcodeResolveKey(postcode, addressHint)
  const cached = postcodeResolveSessionCache.get(cacheKey)

  if (cached) {
    return cached
  }

  const response = await axiosInstance.get("/address/resolve", {
    params: {
      postcode,
      addressHint: addressHint?.trim() || undefined,
    },
    skipAuthRedirect: true,
  })

  const resolved = parseResolveResult(response.data)

  if (resolved) {
    postcodeResolveSessionCache.set(cacheKey, resolved)
  }

  return resolved
}
