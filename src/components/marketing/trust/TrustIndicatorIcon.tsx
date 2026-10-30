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

/** `sm` is the footer tile box (about 43px); `lg` is the carousel box (about 74px). */
const BOX_CLASS = { sm: "size-[43px]", lg: "size-[74px]" } as const

type TrustIndicatorIconProps = {
  iconKey: string | null
  /** When set, wins over `iconKey` and renders as an image. */
  imageUrl: string | null
  /**
   * Part of the tile contract. Not rendered: the wrapper is aria-hidden and the
   * tile text is the accessible name, so the image uses `alt=""`.
   */
  altText: string
  size: keyof typeof BOX_CLASS
}

/**
 * Decorative only: the tile carries the status text, so this is always hidden
 * from assistive technology. Unknown or missing keys show Lucide `Shield`.
 */
export function TrustIndicatorIcon({ iconKey, imageUrl, size }: TrustIndicatorIconProps) {
  const box = cn("inline-flex shrink-0 items-center justify-center", BOX_CLASS[size])

  if (imageUrl !== null) {
    return <span aria-hidden="true" className={box}>{tileImage(imageUrl)}</span>
  }

  const artwork = iconKey === null ? undefined : ARTWORK.get(iconKey)
  if (artwork !== undefined) {
    return <span aria-hidden="true" className={box}>{tileImage(artwork)}</span>
  }

  return (
    <span aria-hidden="true" className={box}>
      {roadmapGlyph(iconKey)}
    </span>
  )
}

/**
 * The one inner image used by both branches. No forced width or height: the
 * image keeps its natural size and only shrinks to fit the box.
 */
function tileImage(src: string): ReactNode {
  return (
    <img
      src={src}
      alt=""
      loading="lazy"
      decoding="async"
      className="max-h-full max-w-full object-contain"
    />
  )
}

/** Neutral Lucide stand-ins for roadmap items (CONTRACT §9). Unknown keys get `Shield`. */
function roadmapGlyph(iconKey: string | null): ReactNode {
  const className = "size-full"
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
