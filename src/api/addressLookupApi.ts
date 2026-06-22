import axiosInstance from "@/api/axiosInstance"
import type {
  AddressResolveResult,
  AddressSuggestion,
} from "@/lib/addressLookup"
import { readBoolean, readString, unwrapDataObject } from "@/lib/apiEnvelope"

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
    address: address ?? label,
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

          if (!premiseAddress) {
            return null
          }

          return {
            address: premiseAddress,
            postcode: premisePostcode ?? postcode,
          }
        })
        .filter((entry): entry is { address: string; postcode: string } =>
          Boolean(entry)
        )
    : []

  return {
    postcode,
    address,
    premises,
    multiplePremises: readBoolean(record, "multiplePremises") ?? premises.length > 1,
    usedBestMatch: readBoolean(record, "usedBestMatch") ?? false,
  }
}

export async function suggestAddresses(query: string) {
  const response = await axiosInstance.get("/address/suggest", {
    params: { q: query },
    skipAuthRedirect: true,
  })

  const payload = response.data as { suggestions?: unknown[] }
  const suggestions = Array.isArray(payload.suggestions)
    ? payload.suggestions
        .map(parseSuggestion)
        .filter((entry): entry is AddressSuggestion => Boolean(entry))
    : []

  return suggestions
}

export async function resolvePostcodeAddress(
  postcode: string,
  addressHint?: string
) {
  const response = await axiosInstance.get("/address/resolve", {
    params: {
      postcode,
      addressHint: addressHint?.trim() || undefined,
    },
    skipAuthRedirect: true,
  })

  return parseResolveResult(response.data)
}
