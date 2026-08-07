import { Button } from "@/components/ui/button"
import { Separator } from "@/components/ui/separator"
import {
  CAMPAIGN_TEMPLATE_CARD_ACTIONS_CLASS,
  CAMPAIGN_TEMPLATE_CARD_CLASS,
  CAMPAIGN_TEMPLATE_CARD_DESCRIPTION_CLASS,
  CAMPAIGN_TEMPLATE_CARD_META_LABEL_CLASS,
  CAMPAIGN_TEMPLATE_CARD_META_ROW_CLASS,
  CAMPAIGN_TEMPLATE_CARD_META_VALUE_CLASS,
  CAMPAIGN_TEMPLATE_CARD_TITLE_CLASS,
  CAMPAIGN_TEMPLATE_PICKER_COPY,
} from "@/lib/operatorCampaigns/campaignTemplatePickerPresentation"
import type { CampaignTemplatePickerCardViewModel } from "@/lib/operatorCampaigns/createCampaignTemplatePickerModule"

type CampaignTemplateCardProps = {
  card: CampaignTemplatePickerCardViewModel
  /** Wired in ticket 28 — Use template opens the wizard. */
  onUseTemplate?: (templateId: string) => void
}

function MetaRow({ label, value }: { label: string; value: string }) {
  return (
    <div className={CAMPAIGN_TEMPLATE_CARD_META_ROW_CLASS}>
      <span className={CAMPAIGN_TEMPLATE_CARD_META_LABEL_CLASS}>{label}</span>
      <span className={CAMPAIGN_TEMPLATE_CARD_META_VALUE_CLASS}>{value}</span>
    </div>
  )
}

/** One catalogue card — Figma Template 4758:75860. Preview stays disabled (ticket 13). */
export function CampaignTemplateCard({
  card,
  onUseTemplate,
}: CampaignTemplateCardProps) {
  const copy = CAMPAIGN_TEMPLATE_PICKER_COPY

  return (
    <article className={CAMPAIGN_TEMPLATE_CARD_CLASS} aria-label={card.title}>
      <div className="flex w-full flex-col gap-[18px]">
        <div className="flex flex-col gap-2">
          <h3 className={CAMPAIGN_TEMPLATE_CARD_TITLE_CLASS}>{card.title}</h3>
          <p className={CAMPAIGN_TEMPLATE_CARD_DESCRIPTION_CLASS}>
            {card.description}
          </p>
        </div>

        <Separator className="bg-op-card-border" />

        <div className="flex flex-col gap-2">
          <MetaRow label={copy.goalMeta} value={card.goalLabel} />
          <MetaRow label={copy.audienceMeta} value={card.audienceLabel} />
          <MetaRow label={copy.channelMeta} value={card.channelLabel} />
          <MetaRow label={copy.offerMeta} value={card.offerLabel} />
        </div>
      </div>

      <div className={CAMPAIGN_TEMPLATE_CARD_ACTIONS_CLASS}>
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
