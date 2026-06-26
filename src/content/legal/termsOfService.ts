import {
  buildFigmaLegalSections,
  LEGAL_PAGE_SUBTITLE,
} from "./figmaCopy"
import type { LegalPageContent } from "./types"

export const termsOfServiceContent: LegalPageContent = {
  title: "Terms of Service",
  description: LEGAL_PAGE_SUBTITLE,
  sections: buildFigmaLegalSections(),
}
