import type { ReactNode } from "react"

export type LegalSection = {
  id: string
  title: string
  content?: ReactNode
}

export type LegalPageContent = {
  title: string
  description: string
  sections: LegalSection[]
}
