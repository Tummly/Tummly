import {
  buildFigmaLegalSections,
  LEGAL_PAGE_SUBTITLE,
} from "./figmaCopy"
import type { LegalPageContent } from "./types"

export const privacyPolicyContent: LegalPageContent = {
  title: "Privacy Policy",
  description: LEGAL_PAGE_SUBTITLE,
  sections: buildFigmaLegalSections(),
}
