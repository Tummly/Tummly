import type { CSSProperties, ReactNode } from "react"

import brandLogoPlaceholder from "@/assets/images/brand-logo-placeholder.png"
import authHeroLogo from "@/assets/images/auth-hero-logo.png"
import { topDecorationPicture } from "@/assets/guest-feedback-images"
import { GuestFeedbackBottomEdge } from "@/components/guest-feedback/GuestFeedbackBottomEdge"
import {
  GUEST_PREVIEW_EMPTY_VALUE,
  GUEST_PREVIEW_FOOTER_COOKIE,
  GUEST_PREVIEW_FOOTER_PRIVACY,
  GUEST_PREVIEW_FOOTER_TERMS,
  GUEST_PREVIEW_FOOTER_UNSUBSCRIBE,
  GUEST_PREVIEW_POWERED_BY_LABEL,
  guestPreviewBrandSubtitle,
  guestPreviewBrandTitle,
  guestPreviewFooterAddress,
  guestPreviewFooterDisclaimer,
} from "@/lib/operatorFeedback/guestPreviewPresentation"
import { pictureToImageSet } from "@/lib/pictureBackground"
import { cn } from "@/lib/utils"

const topDecorationBackground = pictureToImageSet(topDecorationPicture)

/** Dark canvas tokens so the chrome paints outside `html.op`. */
export const NON_TRANSACTIONAL_EMAIL_TOKEN_STYLE = {
  "--op-color-white": "#ffffff",
  "--op-color-black": "#141414",
  "--op-color-gray-550": "#7c7c7c",
  "--op-color-gray-900": "#343434",
  "--op-color-gray-950": "#2c2c2c",
  "--op-color-gray-980": "#262626",
  "--op-color-gray-995": "#1b1b1b",
  "--op-radius-xl": "10px",
  "--radius-op-xl": "10px",
} as CSSProperties

export type BaseNonTransactionalEmailProps = {
  brandName: string | null | undefined
  locationName: string | null
  locationAddress: string | null
  subject: string
  message: string
  offer?: ReactNode
  className?: string
  maxWidthClass?: string
}

function BrandLogo() {
  return (
    <span
      className="relative size-12 shrink-0 overflow-hidden rounded-[2px]"
      aria-hidden
    >
      <img
        src={brandLogoPlaceholder}
        alt=""
        className="size-full object-cover"
      />
    </span>
  )
}

/**
 * Shared Guest-facing (non-transactional) email chrome — brand, ticket,
 * optional offer, legal footer, powered-by + green tear.
 */
export function BaseNonTransactionalEmail({
  brandName,
  locationName,
  locationAddress,
  subject,
  message,
  offer,
  className,
  maxWidthClass = "max-w-[600px]",
}: BaseNonTransactionalEmailProps) {
  const title = guestPreviewBrandTitle(brandName, locationName)
  const subtitle = guestPreviewBrandSubtitle(brandName, locationName)
  const disclaimer = guestPreviewFooterDisclaimer(title)
  const addressLine = guestPreviewFooterAddress(title, locationAddress)
  const trimmedSubject = subject.trim()
  const trimmedMessage = message.trim() || GUEST_PREVIEW_EMPTY_VALUE

  return (
    <div
      className={cn(
        "mx-auto w-full overflow-hidden rounded-[4px] bg-[var(--op-color-black)]",
        maxWidthClass,
        className
      )}
      style={NON_TRANSACTIONAL_EMAIL_TOKEN_STYLE}
    >
      <div
        data-non-transactional-slot="brand"
        className="relative flex flex-col items-start pl-8 pr-[52px] pt-[62px]"
      >
        <div
          aria-hidden
          className="pointer-events-none absolute right-0 top-0 h-[138px] w-[314px] overflow-hidden"
        >
          <div
            className="absolute inset-0"
            style={{
              backgroundImage: topDecorationBackground,
              backgroundRepeat: "no-repeat",
              backgroundSize: "cover",
              backgroundPosition: "right top",
            }}
          />
          <div
            className="absolute inset-0"
            style={{
              backgroundImage:
                "linear-gradient(19.66deg, var(--op-color-black) 25.8%, transparent 110%), linear-gradient(37.61deg, var(--op-color-black) 32.9%, transparent 71.4%)",
            }}
          />
        </div>

        <div className="relative z-10 flex items-center gap-3">
          <BrandLogo />
          <div className="flex min-w-0 flex-col gap-1">
            <p className="m-0 text-[22px] font-semibold leading-normal text-[var(--op-color-white)]">
              {title}
            </p>
            {subtitle != null ? (
              <p className="m-0 text-xs font-semibold leading-normal text-[var(--op-color-white)]/80">
                {subtitle}
              </p>
            ) : null}
          </div>
        </div>
      </div>

      <div className="flex flex-col items-start bg-[var(--op-color-black)] px-8 py-10">
        <div className="relative flex w-full flex-col gap-[30px] rounded-[10px] border border-[var(--op-color-gray-980)] bg-[var(--op-color-gray-995)] p-8">
          <div
            data-non-transactional-slot="ticket"
            className="flex w-full flex-col gap-3"
          >
            {trimmedSubject ? (
              <p className="m-0 text-sm font-semibold leading-5 text-[var(--op-color-white)]">
                {trimmedSubject}
              </p>
            ) : null}
            <p className="m-0 whitespace-pre-wrap text-sm font-normal leading-5 text-[var(--op-color-white)]">
              {trimmedMessage}
            </p>
          </div>

          {offer != null ? (
            <div data-non-transactional-slot="offer">{offer}</div>
          ) : null}

          <span
            aria-hidden
            className="absolute -left-3 top-1/2 size-[18px] -translate-y-1/2 rounded-[20px] bg-[var(--op-color-black)]"
          />
          <span
            aria-hidden
            className="absolute -right-3 top-1/2 size-[18px] -translate-y-1/2 rounded-[20px] bg-[var(--op-color-black)]"
          />
        </div>
      </div>

      <div
        data-non-transactional-slot="legal"
        className="flex flex-col items-center overflow-clip bg-[var(--op-color-black)] px-8 pb-[60px] pt-8"
      >
        <div className="flex w-full flex-col items-center gap-[26px]">
          <div className="mx-auto flex max-w-[440px] flex-col items-center gap-3 text-center">
            <p className="m-0 text-sm font-normal leading-[19px] text-[var(--op-color-white)]">
              {disclaimer}
            </p>
            <p className="m-0 text-xs font-normal leading-normal text-[var(--op-color-white)]">
              {addressLine}
            </p>
          </div>
          <div className="h-px w-full bg-[var(--op-color-gray-980)]" />
          <div className="flex flex-wrap items-center justify-center gap-5 text-xs font-medium text-[var(--op-color-gray-550)]">
            <span>{GUEST_PREVIEW_FOOTER_UNSUBSCRIBE}</span>
            <span>{GUEST_PREVIEW_FOOTER_TERMS}</span>
            <span>{GUEST_PREVIEW_FOOTER_PRIVACY}</span>
            <span>{GUEST_PREVIEW_FOOTER_COOKIE}</span>
          </div>
        </div>
      </div>

      <div
        data-non-transactional-slot="poweredBy"
        className="flex flex-col items-center"
      >
        <div className="mb-2 flex items-start gap-1.5">
          <span className="text-[10px] font-medium leading-normal text-[var(--op-color-white)]">
            {GUEST_PREVIEW_POWERED_BY_LABEL}
          </span>
          <img src={authHeroLogo} alt="Tummly" className="h-[19px] w-auto" />
        </div>
        <GuestFeedbackBottomEdge placement="inline" />
      </div>
    </div>
  )
}
