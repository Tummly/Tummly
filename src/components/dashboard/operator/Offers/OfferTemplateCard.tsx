import { Button } from "@/components/ui/button"
import { Separator } from "@/components/ui/separator"
import type { OfferTemplatePickerCardViewModel } from "@/lib/operatorOffers/createOfferTemplatePickerModule"
import {
  OFFER_TEMPLATE_CARD_ACTIONS_CLASS,
  OFFER_TEMPLATE_CARD_CLASS,
  OFFER_TEMPLATE_CARD_DESCRIPTION_CLASS,
  OFFER_TEMPLATE_CARD_META_LABEL_CLASS,
  OFFER_TEMPLATE_CARD_META_ROW_CLASS,
  OFFER_TEMPLATE_CARD_META_VALUE_CLASS,
  OFFER_TEMPLATE_CARD_TITLE_CLASS,
  OFFER_TEMPLATE_PICKER_COPY,
} from "@/lib/operatorOffers/offerTemplatePickerPresentation"

type OfferTemplateCardProps = {
  card: OfferTemplatePickerCardViewModel
  onUseTemplate?: (templateId: string) => void
}

function MetaRow({ label, value }: { label: string; value: string }) {
  return (
    <div className={OFFER_TEMPLATE_CARD_META_ROW_CLASS}>
      <span className={OFFER_TEMPLATE_CARD_META_LABEL_CLASS}>{label}</span>
      <span className={OFFER_TEMPLATE_CARD_META_VALUE_CLASS}>{value}</span>
    </div>
  )
}

/** One Offer template card — Figma Template 4783:31117. */
export function OfferTemplateCard({
  card,
  onUseTemplate,
}: OfferTemplateCardProps) {
  const copy = OFFER_TEMPLATE_PICKER_COPY
  const hasMeta =
    card.suggestedBenefit != null
    || card.suggestedValidity != null
    || card.suggestedSource != null
    || card.offerTitlePlaceholder != null

  return (
    <article className={OFFER_TEMPLATE_CARD_CLASS} aria-label={card.title}>
      <div className="flex w-full flex-col gap-[18px]">
        <div className="flex flex-col gap-2">
          <h3 className={OFFER_TEMPLATE_CARD_TITLE_CLASS}>{card.title}</h3>
          <p className={OFFER_TEMPLATE_CARD_DESCRIPTION_CLASS}>{card.summary}</p>
        </div>

        {hasMeta ? (
          <>
            <Separator className="bg-op-card-border" />
            <div className="flex flex-col gap-2">
              {card.suggestedBenefit != null ? (
                <MetaRow label={copy.benefitMeta} value={card.suggestedBenefit} />
              ) : null}
              {card.suggestedValidity != null ? (
                <MetaRow
                  label={copy.validityMeta}
                  value={card.suggestedValidity}
                />
              ) : null}
              {card.suggestedSource != null ? (
                <MetaRow label={copy.sourceMeta} value={card.suggestedSource} />
              ) : null}
              {card.offerTitlePlaceholder != null ? (
                <MetaRow
                  label={copy.titlePlaceholderMeta}
                  value={card.offerTitlePlaceholder}
                />
              ) : null}
            </div>
          </>
        ) : null}

        <Separator className="bg-op-card-border" />

        <div className="flex flex-col gap-3 text-xs leading-normal text-[var(--op-color-gray-550)]">
          <span className={OFFER_TEMPLATE_CARD_META_LABEL_CLASS}>
            {copy.startingDescriptionMeta}
          </span>
          <p className="m-0 max-w-[256px] text-xs font-normal leading-4">
            {card.startingDescription}
          </p>
        </div>
      </div>

      <div className={OFFER_TEMPLATE_CARD_ACTIONS_CLASS}>
        <Button
          type="button"
          variant="op-secondary"
          disabled={!card.useTemplateEnabled}
          onClick={() => {
            onUseTemplate?.(card.id)
          }}
        >
          {copy.useTemplate}
        </Button>
        <Button
          type="button"
          variant="op-tertiary"
          disabled={card.previewDisabled}
          aria-disabled={card.previewDisabled}
        >
          {copy.preview}
        </Button>
      </div>
    </article>
  )
}
