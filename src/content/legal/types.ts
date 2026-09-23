import type { ReactNode } from "react"

export type LegalDocumentKey = "privacy" | "terms" | "cookie-policy"

export type LegalSection = {
  id: string
  title: string
  content?: ReactNode
}

export type LegalPageContent = {
  title: string
  /** Hero intro; string or paragraph list. */
  description: string | readonly string[]
  /** Shown as “Last updated: …”. Omit when the date is not ready. */
  lastUpdated?: string
  /** Cookie Notice hero control that opens cookie settings. */
  showManageCookiePreferences?: boolean
  /** When set, show the download control for the matching DOCX. */
  documentKey?: LegalDocumentKey
  sections: LegalSection[]
}
