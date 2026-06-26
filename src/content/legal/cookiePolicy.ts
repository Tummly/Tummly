import {
  buildFigmaLegalSections,
  LEGAL_PAGE_SUBTITLE,
} from "./figmaCopy"
import type { LegalPageContent } from "./types"

export const cookiePolicyContent: LegalPageContent = {
  title: "Cookie Policy",
  description: LEGAL_PAGE_SUBTITLE,
  sections: buildFigmaLegalSections(),
}
