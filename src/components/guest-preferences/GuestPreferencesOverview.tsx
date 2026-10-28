import { useReducer, useState } from "react"
import { toast } from "sonner"

import {
  GuestPreferencesSessionExpiredError,
  stopAllMarketing,
  updateChannels,
  updateFirstName,
  type GuestPreferencesOverview as GuestPreferencesOverviewData,
} from "@/api/publicGuestPreferencesApi"
import { GuestPreferencesChannels } from "@/components/guest-preferences/GuestPreferencesChannels"
import { GuestPreferencesHeader } from "@/components/guest-preferences/GuestPreferencesHeader"
import { GuestPreferencesNameRow } from "@/components/guest-preferences/GuestPreferencesNameRow"
import { GuestPreferencesStopAllDialog } from "@/components/guest-preferences/GuestPreferencesStopAllDialog"
import { Button } from "@/components/ui/button"
import { Separator } from "@/components/ui/separator"
import { GUEST_PREFERENCES_COPY } from "@/content/guestPreferences/guestPreferencesCopy"
import { GUEST_FEEDBACK_PRIMARY_BUTTON_CLASS } from "@/lib/guestFeedback/guestFeedbackLayoutPresentation"
import {
  commitChannelChanges,
  commitFirstName,
  createGuestPreferencesOverviewState,
  guestPreferencesErrorMessage,
  guestPreferencesOverviewReducer,
} from "@/lib/guestPreferences/guestPreferencesOverviewState"
import {
  GUEST_PREFERENCES_CARD_CLASS,
  GUEST_PREFERENCES_CARD_TITLE_CLASS,
  GUEST_PREFERENCES_FIELD_LABEL_CLASS,
  GUEST_PREFERENCES_FIELD_VALUE_CLASS,
  GUEST_PREFERENCES_LINK_CLASS,
  GUEST_PREFERENCES_TEXT_ACTION_CLASS,
} from "@/lib/guestPreferences/guestPreferencesPresentation"
import { cn } from "@/lib/utils"

type GuestPreferencesOverviewProps = {
  overview: GuestPreferencesOverviewData
  /** Called when a request returns 401; the page falls back to the entry view. */
  onSessionExpired: () => void
  onAddContact: () => void
  onManageRestaurants: () => void
}

type PendingAction = "name" | "channels" | "stop-all" | null

/**
 * Logged-in preference view: name, verified contact, channels, restaurant
 * entry point, Save preferences and Stop all.
 */
export function GuestPreferencesOverview({
  overview,
  onSessionExpired,
  onAddContact,
  onManageRestaurants,
}: GuestPreferencesOverviewProps) {
  const [state, dispatch] = useReducer(
    guestPreferencesOverviewReducer,
    overview,
    createGuestPreferencesOverviewState
  )
  const [pending, setPending] = useState<PendingAction>(null)
  const busy = pending !== null

  function handleRequestError(error: unknown) {
    if (error instanceof GuestPreferencesSessionExpiredError) {
      onSessionExpired()
      return
    }
    toast.error(
      guestPreferencesErrorMessage(error, GUEST_PREFERENCES_COPY.saveFailed)
    )
  }

  async function handleSaveName() {
    if (state.nameDraft === null) {
      return
    }
    setPending("name")
    try {
      const firstName = await commitFirstName(
        updateFirstName,
        state.firstName,
        state.nameDraft
      )
      dispatch({ type: "name-saved", firstName })
    } catch (error) {
      handleRequestError(error)
    } finally {
      setPending(null)
    }
  }

  async function handleSaveChannels() {
    setPending("channels")
    try {
      await commitChannelChanges(
        updateChannels,
        state.savedChannels,
        state.channels
      )
      dispatch({ type: "channels-saved", channels: state.channels })
      toast.success(GUEST_PREFERENCES_COPY.preferencesSaved)
    } catch (error) {
      handleRequestError(error)
    } finally {
      setPending(null)
    }
  }

  async function handleConfirmStopAll() {
    setPending("stop-all")
    try {
      await stopAllMarketing()
      dispatch({ type: "stop-all-confirmed" })
    } catch (error) {
      handleRequestError(error)
    } finally {
      setPending(null)
    }
  }

  return (
    <>
      <GuestPreferencesHeader lead={GUEST_PREFERENCES_COPY.lead} />

      <div className="flex w-full flex-col gap-3.5">
        <section className={GUEST_PREFERENCES_CARD_CLASS}>
          <h2 className={GUEST_PREFERENCES_CARD_TITLE_CLASS}>
            {GUEST_PREFERENCES_COPY.detailsTitle}
          </h2>
          <GuestPreferencesNameRow
            firstName={state.firstName}
            draft={state.nameDraft}
            busy={pending === "name"}
            onEdit={() => dispatch({ type: "name-edit-started" })}
            onDraftChange={(value) =>
              dispatch({ type: "name-draft-changed", value })
            }
            onSave={() => void handleSaveName()}
          />
          <Separator className="bg-guest-feedback-border" />
          <div className="flex w-full flex-col gap-6">
            <div className="flex w-full flex-col gap-1">
              <span className={GUEST_PREFERENCES_FIELD_LABEL_CLASS}>
                {GUEST_PREFERENCES_COPY.verifiedContactLabel}
              </span>
              {overview.contacts.map((contact) => (
                <span
                  key={contact.id}
                  className={GUEST_PREFERENCES_FIELD_VALUE_CLASS}
                >
                  {contact.masked}
                </span>
              ))}
            </div>
            <Button
              type="button"
              variant="link"
              size="link-sm"
              onClick={onAddContact}
              className={cn(GUEST_PREFERENCES_LINK_CLASS, "self-start")}
            >
              {GUEST_PREFERENCES_COPY.addContactLabel}
            </Button>
          </div>
        </section>

        <section className={GUEST_PREFERENCES_CARD_CLASS}>
          <h2 className={GUEST_PREFERENCES_CARD_TITLE_CLASS}>
            {GUEST_PREFERENCES_COPY.channelsTitle}
          </h2>
          <GuestPreferencesChannels
            channels={state.channels}
            disabled={busy}
            onChange={(channel, checked) =>
              dispatch({ type: "channel-toggled", channel, checked })
            }
          />
        </section>

        <section className={GUEST_PREFERENCES_CARD_CLASS}>
          <div className="flex w-full flex-col gap-2">
            <h2 className={GUEST_PREFERENCES_CARD_TITLE_CLASS}>
              {GUEST_PREFERENCES_COPY.restaurantsTitle}
            </h2>
            <p className="m-0 font-sans text-xs leading-[17px] text-guest-feedback-muted-soft">
              {GUEST_PREFERENCES_COPY.restaurantsDescription}
            </p>
          </div>
          <Button
            type="button"
            variant="link"
            size="link-sm"
            onClick={onManageRestaurants}
            className={cn(GUEST_PREFERENCES_LINK_CLASS, "self-start text-xs")}
          >
            {GUEST_PREFERENCES_COPY.manageRestaurantsLabel}
          </Button>
        </section>
      </div>

      <Button
        type="button"
        disabled={busy}
        onClick={() => void handleSaveChannels()}
        className={cn(
          GUEST_FEEDBACK_PRIMARY_BUTTON_CLASS,
          "bg-guest-feedback-submit text-guest-feedback-submit-fg hover:bg-white"
        )}
      >
        {GUEST_PREFERENCES_COPY.savePreferencesLabel}
      </Button>

      <Button
        type="button"
        variant="link"
        size="link-sm"
        onClick={() => dispatch({ type: "stop-all-opened" })}
        className={GUEST_PREFERENCES_TEXT_ACTION_CLASS}
      >
        {GUEST_PREFERENCES_COPY.stopAllLabel}
      </Button>

      <GuestPreferencesStopAllDialog
        open={state.stopAllOpen}
        busy={pending === "stop-all"}
        onOpenChange={(open) =>
          dispatch({ type: open ? "stop-all-opened" : "stop-all-closed" })
        }
        onKeep={() => dispatch({ type: "stop-all-closed" })}
        onConfirm={() => void handleConfirmStopAll()}
      />
    </>
  )
}
