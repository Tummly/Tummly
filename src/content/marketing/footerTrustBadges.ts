import aiHuman from "@/assets/svg/marketing/footer-trust-badges/ai-human.svg"
import cyberEssentialsPlus from "@/assets/svg/marketing/footer-trust-badges/cyber-essentials-plus.svg"
import cyberEssentials from "@/assets/svg/marketing/footer-trust-badges/cyber-essentials.svg"
import dataRights from "@/assets/svg/marketing/footer-trust-badges/data-rights.svg"
import gbcCharter from "@/assets/svg/marketing/footer-trust-badges/gbc-charter.svg"
import gdprUk from "@/assets/svg/marketing/footer-trust-badges/gdpr-uk.svg"
import httpsEncrypted from "@/assets/svg/marketing/footer-trust-badges/https-encrypted.svg"
import iasmeLevel1 from "@/assets/svg/marketing/footer-trust-badges/iasme-level-1.svg"
import icoData from "@/assets/svg/marketing/footer-trust-badges/ico-data.svg"
import icoDpe from "@/assets/svg/marketing/footer-trust-badges/ico-dpe.svg"
import iso27001 from "@/assets/svg/marketing/footer-trust-badges/iso-27001.svg"
import iso27701 from "@/assets/svg/marketing/footer-trust-badges/iso-27701.svg"
import optInMarketing from "@/assets/svg/marketing/footer-trust-badges/opt-in-marketing.svg"
import privacyChoices from "@/assets/svg/marketing/footer-trust-badges/privacy-choices.svg"
import ukCompany from "@/assets/svg/marketing/footer-trust-badges/uk-company.svg"

export type FooterTrustBadge = {
  /** Backend trust indicator catalog number (`TrustIndicatorCatalogSeed`). */
  itemNumber: number
  title: string
  src: string
  /** Root size of the exported SVG. */
  width: number
  height: number
}

/** “Built with trust in mind” badge grid, in Figma order (`5839:73487`). */
export const FOOTER_TRUST_BADGES: readonly FooterTrustBadge[] = [
  { itemNumber: 5, title: "ISO/IEC 27001", src: iso27001, width: 43, height: 43 },
  { itemNumber: 4, title: "Cyber Essentials Plus", src: cyberEssentialsPlus, width: 43, height: 43 },
  { itemNumber: 2, title: "ICO Data Protection Essentials", src: icoDpe, width: 36, height: 43 },
  { itemNumber: 1, title: "ICO Data Protection Registration", src: icoData, width: 42, height: 42 },
  { itemNumber: 3, title: "Cyber Essentials", src: cyberEssentials, width: 38, height: 43 },
  { itemNumber: 6, title: "IASME Cyber Assurance Level One", src: iasmeLevel1, width: 41, height: 41 },
  { itemNumber: 7, title: "Good Business Charter", src: gbcCharter, width: 39, height: 48 },
  { itemNumber: 8, title: "ISO/IEC 27701:2025", src: iso27701, width: 43, height: 43 },
  { itemNumber: 9, title: "UK GDPR & Data Protection", src: gdprUk, width: 34, height: 43 },
  { itemNumber: 11, title: "Permission-Based Marketing", src: optInMarketing, width: 36, height: 42 },
  { itemNumber: 14, title: "Encrypted HTTPS Connections", src: httpsEncrypted, width: 41, height: 45 },
  { itemNumber: 12, title: "Guest Data Rights", src: dataRights, width: 43, height: 42 },
  { itemNumber: 10, title: "Cookie & Privacy Choices", src: privacyChoices, width: 38, height: 38 },
  { itemNumber: 15, title: "Human-Controlled AI", src: aiHuman, width: 41, height: 36 },
  { itemNumber: 13, title: "UK Registered Company", src: ukCompany, width: 41, height: 49 },
]
