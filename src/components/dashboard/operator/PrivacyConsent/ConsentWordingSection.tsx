import { useEffect, useState } from "react"

import { Button } from "@/components/ui/button"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import {
  GUESTS_SECTION_SUBTITLE_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import {
  PRIVACY_CONSENT_CARD_CLASS,
  PRIVACY_CONSENT_PAGE_COPY,
} from "@/lib/operatorPrivacyConsent/privacyConsentPresentation"
import { cn } from "@/lib/utils"

type ConsentWordingSectionProps = {
  emailWording: string
  smsWording: string
  emailEnabled: boolean
  smsEnabled: boolean
  readOnly: boolean
  onSave: (input: {
    emailConsentWording?: string
    smsConsentWording?: string
  }) => Promise<void>
}

/** Consent wording editor — Privacy setup tab (below status table). */
export function ConsentWordingSection({
  emailWording,
  smsWording,
  emailEnabled,
  smsEnabled,
  readOnly,
  onSave,
}: ConsentWordingSectionProps) {
  const copy = PRIVACY_CONSENT_PAGE_COPY
  const [draftEmail, setDraftEmail] = useState(emailWording)
  const [draftSms, setDraftSms] = useState(smsWording)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    setDraftEmail(emailWording)
  }, [emailWording])

  useEffect(() => {
    setDraftSms(smsWording)
  }, [smsWording])

  if (!emailEnabled && !smsEnabled) {
    return null
  }

  const hasChanges =
    (emailEnabled && draftEmail !== emailWording)
    || (smsEnabled && draftSms !== smsWording)

  return (
    <section
      className={cn(PRIVACY_CONSENT_CARD_CLASS, "gap-10")}
      aria-label={copy.consentWordingTitle}
    >
      <header className="flex flex-col gap-2 leading-[0]">
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.consentWordingTitle}</h2>
        <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
          {copy.consentWordingSubtitle}
        </p>
      </header>

      <div className="flex w-full flex-col gap-6">
        {emailEnabled ? (
          <div className="flex flex-col gap-2">
            <Label htmlFor="privacy-email-consent-wording">
              {copy.emailConsentWordingLabel}
            </Label>
            <Textarea
              id="privacy-email-consent-wording"
              value={draftEmail}
              readOnly={readOnly}
              disabled={readOnly || saving}
              rows={4}
              onChange={(event) => {
                setDraftEmail(event.target.value)
              }}
            />
          </div>
        ) : null}

        {smsEnabled ? (
          <div className="flex flex-col gap-2">
            <Label htmlFor="privacy-sms-consent-wording">
              {copy.smsConsentWordingLabel}
            </Label>
            <Textarea
              id="privacy-sms-consent-wording"
              value={draftSms}
              readOnly={readOnly}
              disabled={readOnly || saving}
              rows={4}
              onChange={(event) => {
                setDraftSms(event.target.value)
              }}
            />
          </div>
        ) : null}

        {!readOnly ? (
          <div>
            <Button
              type="button"
              variant="op-primary"
              className="rounded-[2px]"
              disabled={!hasChanges || saving}
              aria-busy={saving}
              onClick={() => {
                setSaving(true)
                void onSave({
                  ...(emailEnabled
                    ? { emailConsentWording: draftEmail }
                    : {}),
                  ...(smsEnabled ? { smsConsentWording: draftSms } : {}),
                }).finally(() => {
                  setSaving(false)
                })
              }}
            >
              {copy.consentWordingSave}
            </Button>
          </div>
        ) : null}
      </div>
    </section>
  )

}
