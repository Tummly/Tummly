import type { ReactNode } from "react"

export type LegalDocumentKey = "privacy" | "terms" | "cookie-policy"

export type LegalSection = {
  id: string
  title: string
  content?: ReactNode
}

export type LegalPageContent = {
  title: string
  description: string
  documentKey: LegalDocumentKey
  sections: LegalSection[]
}
