import axiosInstance from "@/api/axiosInstance"
import type { CompanySuggestion } from "@/lib/companySearch"
import { COMPANY_SUGGEST_MIN_CHARS } from "@/lib/companySearch"
import { readString, unwrapDataObject } from "@/lib/apiEnvelope"
import { isAxiosError } from "axios"

const suggestSessionCache = new Map<string, CompanySuggestion[]>()

function normalizeSuggestQuery(query: string) {
  return query.trim().toLowerCase()
}

export function isCompanySearchAbortError(error: unknown) {
  return (
    (error instanceof DOMException && error.name === "AbortError")
    || (isAxiosError(error) && error.code === "ERR_CANCELED")
  )
}

function parseSuggestion(value: unknown): CompanySuggestion | null {
  const record = unwrapDataObject(value)

  if (!record) {
    return null
  }

  const companyNumber = readString(record, "companyNumber")
  const title = readString(record, "title")

  if (!companyNumber || !title) {
    return null
  }

  return {
    companyNumber,
    title,
    addressSnippet: readString(record, "addressSnippet") ?? "",
    companyStatus: readString(record, "companyStatus") ?? "",
  }
}

export async function suggestCompanies(
  query: string,
  signal?: AbortSignal
) {
  const normalizedQuery = normalizeSuggestQuery(query)

  if (normalizedQuery.length < COMPANY_SUGGEST_MIN_CHARS) {
    return []
  }

  const cached = suggestSessionCache.get(normalizedQuery)

  if (cached) {
    return cached
  }

  const response = await axiosInstance.get("/companies/suggest", {
    params: { q: query.trim() },
    signal,
  })

  const payload = response.data as { suggestions?: unknown[] }
  const suggestions = Array.isArray(payload.suggestions)
    ? payload.suggestions
        .map(parseSuggestion)
        .filter((entry): entry is CompanySuggestion => Boolean(entry))
    : []

  suggestSessionCache.set(normalizedQuery, suggestions)

  return suggestions
}
