import type { ReactNode } from "react"
import {
  ClipboardList,
  FileLock2,
  Handshake,
  Shield,
  ShieldPlus,
  UserLock,
} from "lucide-react"

import aiHuman from "@/assets/svg/marketing/footer-trust-badges/ai-human.svg"
import dataRights from "@/assets/svg/marketing/footer-trust-badges/data-rights.svg"
import gdprUk from "@/assets/svg/marketing/footer-trust-badges/gdpr-uk.svg"
import httpsEncrypted from "@/assets/svg/marketing/footer-trust-badges/https-encrypted.svg"
import icoData from "@/assets/svg/marketing/footer-trust-badges/ico-data.svg"
import icoDpe from "@/assets/svg/marketing/footer-trust-badges/ico-dpe.svg"
import optInMarketing from "@/assets/svg/marketing/footer-trust-badges/opt-in-marketing.svg"
import privacyChoices from "@/assets/svg/marketing/footer-trust-badges/privacy-choices.svg"
import ukCompany from "@/assets/svg/marketing/footer-trust-badges/uk-company.svg"
import { cn } from "@/lib/utils"

/** Existing footer artwork, keyed by CONTRACT §9 icon key. */
const ARTWORK = new Map<string, string>([
  ["ico-data", icoData],
  ["ico-dpe", icoDpe],
  ["gdpr-uk", gdprUk],
  ["privacy-choices", privacyChoices],
  ["opt-in-marketing", optInMarketing],
  ["data-rights", dataRights],
  ["uk-company", ukCompany],
  ["https-encrypted", httpsEncrypted],
  ["ai-human", aiHuman],
])

type TrustIndicatorIconProps = {
  iconKey: string | null
  className?: string
}

/**
 * Decorative only: the tile carries the status text, so this is always hidden
 * from assistive technology. Unknown or missing keys show Lucide `Shield`.
 */
export function TrustIndicatorIcon({ iconKey, className }: TrustIndicatorIconProps) {
  const frame = cn("inline-flex size-10 shrink-0 items-center justify-center", className)
  const artwork = iconKey === null ? undefined : ARTWORK.get(iconKey)

  if (artwork !== undefined) {
    return (
      <span aria-hidden="true" className={frame}>
        <img src={artwork} alt="" className="h-full w-full object-contain" />
      </span>
    )
  }

  return (
    <span aria-hidden="true" className={frame}>
      {roadmapGlyph(iconKey)}
    </span>
  )
}

/** Neutral Lucide stand-ins for roadmap items (CONTRACT §9). Unknown keys get `Shield`. */
function roadmapGlyph(iconKey: string | null): ReactNode {
  const className = "h-full w-full"
  switch (iconKey) {
    case "roadmap-cyber-essentials-plus":
      return <ShieldPlus aria-hidden="true" className={className} />
    case "roadmap-iso-27001":
      return <FileLock2 aria-hidden="true" className={className} />
    case "roadmap-iasme":
      return <ClipboardList aria-hidden="true" className={className} />
    case "roadmap-gbc":
      return <Handshake aria-hidden="true" className={className} />
    case "roadmap-iso-27701":
      return <UserLock aria-hidden="true" className={className} />
    default:
      return <Shield aria-hidden="true" className={className} />
  }
}
