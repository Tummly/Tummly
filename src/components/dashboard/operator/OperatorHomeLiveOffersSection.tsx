import { Button } from "@/components/ui/button"
import {
  LIVE_OFFERS_EMPTY_ACTIONS,
  LIVE_OFFERS_EMPTY_COPY,
  LIVE_OFFERS_EMPTY_HELPER,
  LIVE_OFFERS_EMPTY_ACTIONS_CLASS,
  LIVE_OFFERS_EMPTY_ACTION_BUTTON_CLASS,
  LIVE_OFFERS_EMPTY_COPY_CLASS,
  LIVE_OFFERS_EMPTY_HELPER_CLASS,
  LIVE_OFFERS_EMPTY_SHELL_CLASS,
  LIVE_OFFERS_EMPTY_TITLE_CLASS,
  LIVE_OFFERS_HEADER_CLASS,
  LIVE_OFFERS_SECTION_CLASS,
  LIVE_OFFERS_SUBTITLE_CLASS,
  LIVE_OFFERS_TITLE_CLASS,
  resolveLiveOffersEmptyActionVariant,
} from "@/lib/operatorHome/liveOffersSectionPresentation"

/** Figma Live offers and campaigns — component-owned empty shell with disabled CTAs. */
export function OperatorHomeLiveOffersSection() {
  return (
    <section className={LIVE_OFFERS_SECTION_CLASS}>
      <div className={LIVE_OFFERS_HEADER_CLASS}>
        <h2 className={LIVE_OFFERS_TITLE_CLASS}>Live offers and campaigns</h2>
        <p className={LIVE_OFFERS_SUBTITLE_CLASS}>
          See what is currently running and how it is performing.
        </p>
      </div>
      <div className={LIVE_OFFERS_EMPTY_SHELL_CLASS}>
        <div className={LIVE_OFFERS_EMPTY_COPY_CLASS}>
          <p className={LIVE_OFFERS_EMPTY_TITLE_CLASS}>{LIVE_OFFERS_EMPTY_COPY}</p>
          <p className={LIVE_OFFERS_EMPTY_HELPER_CLASS}>{LIVE_OFFERS_EMPTY_HELPER}</p>
        </div>
        <div className={LIVE_OFFERS_EMPTY_ACTIONS_CLASS}>
          {LIVE_OFFERS_EMPTY_ACTIONS.map((action) => (
            <Button
              key={action.id}
              type="button"
              variant={resolveLiveOffersEmptyActionVariant(action.id)}
              size="sm"
              className={LIVE_OFFERS_EMPTY_ACTION_BUTTON_CLASS}
              disabled
              aria-disabled
              aria-label={`${action.label} (unavailable)`}
            >
              {action.label}
            </Button>
          ))}
        </div>
      </div>
    </section>
  )
}
