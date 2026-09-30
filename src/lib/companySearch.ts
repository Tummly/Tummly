export const COMPANY_SUGGEST_MIN_CHARS = 4
export const COMPANY_SUGGEST_DEBOUNCE_MS = 500
export const COMPANY_USE_MY_LEGAL_NAME_LABEL = "Use my legal name instead"

export type CompanySuggestion = {
  companyNumber: string
  title: string
  addressSnippet: string
  companyStatus: string
}
