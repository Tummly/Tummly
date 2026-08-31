import type { ReactNode } from "react"

import { Badge } from "@/components/ui/badge"
import { Separator } from "@/components/ui/separator"
import { Switch } from "@/components/ui/switch"
import {
  GUESTS_MARKETING_STATUS_BADGE_CLASS,
  GUESTS_SECTION_SUBTITLE_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import {
  GUEST_PERMISSION_SWITCH_CLASS,
  GUEST_PERMISSION_TILE_CLASS,
  GUEST_PERMISSION_TILE_GRID_CLASS,
  PRIVACY_CONSENT_CARD_CLASS,
  PRIVACY_CONSENT_PAGE_COPY,
  guestPermissionStatusLabel,
  type GuestPermissionCard,
  type GuestPermissionId,
} from "@/lib/operatorPrivacyConsent/privacyConsentPresentation"
import { cn } from "@/lib/utils"

type GuestPermissionsSectionProps = {
  cards: readonly GuestPermissionCard[]
  onEnabledChange: (id: GuestPermissionId, enabled: boolean) => void
}

function GuestPermissionMetaRow({
  label,
  children,
}: {
  label: string
  children: ReactNode
}) {
  return (
    <div className="flex flex-wrap items-center gap-3.5 text-sm font-medium leading-normal">
      <span className="text-[var(--op-color-gray-550)]">{label}</span>
      {children}
    </div>
  )
}

/** Guest permissions card — Figma 5746:100280. */
export function GuestPermissionsSection({
  cards,
  onEnabledChange,
}: GuestPermissionsSectionProps) {
  const copy = PRIVACY_CONSENT_PAGE_COPY

  return (
    <section
      className={cn(PRIVACY_CONSENT_CARD_CLASS, "gap-10")}
      aria-label={copy.guestPermissionsTitle}
    >
      <header className="flex flex-col gap-2 leading-[0]">
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>
          {copy.guestPermissionsTitle}
        </h2>
        <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
          {copy.guestPermissionsSubtitle}
        </p>
      </header>

      <div className={GUEST_PERMISSION_TILE_GRID_CLASS}>
        {cards.map((card) => {
          const status = guestPermissionStatusLabel(card.enabled)
          const switchId = `guest-permission-${card.id}`

          return (
            <article
              key={card.id}
              className={GUEST_PERMISSION_TILE_CLASS}
              aria-labelledby={`${switchId}-title`}
            >
              <div className="flex flex-col gap-3">
                <div className="flex items-center gap-3.5">
                  <Switch
                    id={switchId}
                    checked={card.enabled}
                    onCheckedChange={(checked) => {
                      onEnabledChange(card.id, checked)
                    }}
                    className={GUEST_PERMISSION_SWITCH_CLASS}
                    aria-labelledby={`${switchId}-title`}
                  />
                  <h3
                    id={`${switchId}-title`}
                    className="m-0 text-lg font-medium leading-normal text-op-card-title-color"
                  >
                    {card.title}
                  </h3>
                </div>
                <p className="m-0 text-sm font-medium leading-normal text-[var(--op-color-gray-550)]">
                  {card.description}
                </p>
              </div>

              <Separator className="bg-op-card-border" />

              <div className="flex flex-col gap-[22px]">
                <GuestPermissionMetaRow label={copy.statusLabel}>
                  <Badge
                    variant="soft"
                    className={GUESTS_MARKETING_STATUS_BADGE_CLASS}
                  >
                    {status}
                  </Badge>
                </GuestPermissionMetaRow>
                <GuestPermissionMetaRow label={copy.usedInLabel}>
                  <span className="text-op-card-title-color">{card.usedIn}</span>
                </GuestPermissionMetaRow>
                <GuestPermissionMetaRow label={copy.collectedThroughLabel}>
                  <span className="text-op-card-title-color">
                    {card.collectedThrough}
                  </span>
                </GuestPermissionMetaRow>
              </div>
            </article>
          )
        })}
      </div>

      <p className="m-0 text-base font-medium leading-normal text-[var(--op-color-gray-550)]">
        {copy.guestPermissionsFootnote}
      </p>
    </section>
  )
}
