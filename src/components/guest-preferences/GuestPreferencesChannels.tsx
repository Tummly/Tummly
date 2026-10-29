import { CheckboxLabel } from "@/components/ui/checkbox-label"
import { GUEST_PREFERENCES_COPY } from "@/content/guestPreferences/guestPreferencesCopy"
import type { GuestPreferenceChannels } from "@/lib/guestPreferences/guestPreferencesChannels"

type GuestPreferencesChannelsProps = {
  channels: GuestPreferenceChannels
  disabled: boolean
  onChange: (channel: keyof GuestPreferenceChannels, checked: boolean) => void
}

const CHANNEL_LABEL_CLASS =
  "cursor-pointer font-sans text-xs leading-4 text-guest-feedback-text"

/** Email and SMS marketing checkboxes. The card heading is rendered by the caller. */
export function GuestPreferencesChannels({
  channels,
  disabled,
  onChange,
}: GuestPreferencesChannelsProps) {
  return (
    <div className="flex w-full flex-col gap-6">
      <CheckboxLabel
        id="guest-preferences-email"
        variant="ghost"
        checked={channels.email}
        disabled={disabled}
        onCheckedChange={(checked) => onChange("email", checked)}
        labelClassName={CHANNEL_LABEL_CLASS}
      >
        <ChannelText
          title={GUEST_PREFERENCES_COPY.emailLabel}
          description={GUEST_PREFERENCES_COPY.emailDescription}
        />
      </CheckboxLabel>
      <CheckboxLabel
        id="guest-preferences-sms"
        variant="ghost"
        checked={channels.sms}
        disabled={disabled}
        onCheckedChange={(checked) => onChange("sms", checked)}
        labelClassName={CHANNEL_LABEL_CLASS}
      >
        <ChannelText
          title={GUEST_PREFERENCES_COPY.smsLabel}
          description={GUEST_PREFERENCES_COPY.smsDescription}
        />
      </CheckboxLabel>
    </div>
  )
}

function ChannelText({
  title,
  description,
}: {
  title: string
  description: string
}) {
  return (
    <span className="flex flex-col gap-0.5">
      <span className="font-semibold text-guest-feedback-text">{title}</span>
      <span className="font-medium text-guest-feedback-muted-soft">
        {description}
      </span>
    </span>
  )
}
