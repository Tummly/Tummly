import { MarketingLogo } from "@/components/marketing/MarketingLogo"
import { GUEST_PREFERENCES_COPY } from "@/content/guestPreferences/guestPreferencesCopy"

type GuestPreferencesHeaderProps = {
  lead: string
}

/** Tummly wordmark, page heading and one line of supporting copy. */
export function GuestPreferencesHeader({ lead }: GuestPreferencesHeaderProps) {
  return (
    <header className="flex w-full flex-col gap-[34px] px-5 pt-5 pb-2.5">
      <MarketingLogo
        onLight={false}
        width={114}
        height={28}
        className="h-7 w-[113.78px]"
      />
      <div className="flex flex-col gap-3">
        <h1 className="m-0 font-heading text-2xl font-bold leading-7 text-guest-feedback-text">
          {GUEST_PREFERENCES_COPY.title}
        </h1>
        <p className="m-0 font-sans text-sm leading-body text-guest-feedback-muted-soft">
          {lead}
        </p>
      </div>
    </header>
  )
}
