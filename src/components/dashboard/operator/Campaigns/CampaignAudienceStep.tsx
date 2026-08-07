import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import type { CampaignAudienceId } from "@/lib/operatorCampaigns/campaignAudiencePresentation"
import { CAMPAIGN_AUDIENCE_COPY } from "@/lib/operatorCampaigns/campaignAudiencePresentation"
import { CAMPAIGN_WIZARD_SELECT_MENU_CLASS } from "@/lib/operatorCampaigns/campaignWizardPresentation"
import type {
  CampaignAudienceOptionViewModel,
  CampaignAudienceViewModel,
} from "@/lib/operatorCampaigns/createCampaignWizardModule"
import { cn } from "@/lib/utils"

type CampaignAudienceStepProps = {
  audience: CampaignAudienceViewModel
  onSelectAudience: (audienceId: CampaignAudienceId) => void
  onSelectSavedGroup: (savedGroupId: string | null) => void
}

function AudienceOptionCard({
  option,
  onSelect,
}: {
  option: CampaignAudienceOptionViewModel
  onSelect: () => void
}) {
  return (
    <Button
      type="button"
      variant="ghost"
      role="radio"
      aria-checked={option.selected}
      className={cn(
        "h-auto min-h-0 w-full flex-col items-start gap-4 rounded-[4px] border p-[22px] text-left whitespace-normal hover:bg-transparent",
        option.selected
          ? "border-[var(--op-color-gray-550)] bg-op-background-primary"
          : "border-op-card-border bg-op-background-primary hover:border-[var(--op-color-gray-550)]"
      )}
      onClick={onSelect}
    >
      <span className="flex w-full flex-col gap-2">
        <span className="flex flex-wrap items-center gap-3.5">
          <span className="text-base font-medium leading-normal text-op-text-primary">
            {option.title}
          </span>
          {option.recommended ? (
            <Badge
              variant="soft"
              className="rounded-[2px] bg-op-color-green-500-muted px-1.5 py-1 text-xs font-medium text-op-color-green-500"
            >
              {CAMPAIGN_AUDIENCE_COPY.recommendedBadge}
            </Badge>
          ) : null}
        </span>
        <span className="text-xs font-medium leading-[18px] text-[var(--op-color-gray-550)]">
          {option.description}
        </span>
      </span>
      <Badge variant="soft" className="rounded-[2px] px-1.5 py-1 text-xs font-medium">
        {option.countLabel}
      </Badge>
    </Button>
  )
}

function EligibilitySummary({
  audience,
}: {
  audience: CampaignAudienceViewModel
}) {
  const breakdown = audience.eligibilityBreakdown
  const rows = [
    {
      label: CAMPAIGN_AUDIENCE_COPY.matchedLabel,
      value: breakdown.matched,
    },
    {
      label: CAMPAIGN_AUDIENCE_COPY.currentlyEligibleLabel,
      value: breakdown.currentlyEligible,
    },
    {
      label: CAMPAIGN_AUDIENCE_COPY.excludedLabel,
      value: breakdown.excluded,
    },
    {
      label: CAMPAIGN_AUDIENCE_COPY.emailEligibleLabel,
      value: breakdown.emailEligible,
    },
    {
      label: CAMPAIGN_AUDIENCE_COPY.smsEligibleLabel,
      value: breakdown.smsEligible,
    },
  ] as const

  return (
    <aside
      className="flex w-full shrink-0 flex-col gap-6 rounded-[4px] border border-op-card-border bg-op-background-primary p-5 lg:w-[min(100%,560px)]"
      aria-label={CAMPAIGN_AUDIENCE_COPY.summaryTitle}
      data-eligibility-source={breakdown.source}
    >
      <h3 className="m-0 text-lg font-semibold leading-normal text-op-text-primary">
        {CAMPAIGN_AUDIENCE_COPY.summaryTitle}
      </h3>
      <dl className="m-0 flex w-full flex-col gap-3.5">
        {rows.map((row, index) => (
          <div key={row.label} className="flex flex-col gap-3.5">
            <div className="flex items-center justify-between gap-3 text-sm">
              <dt className="m-0 font-semibold text-[var(--op-color-gray-550)]">
                {row.label}
              </dt>
              <dd className="m-0 font-medium text-op-text-primary">
                {row.value.toLocaleString("en-GB")}
              </dd>
            </div>
            {index < rows.length - 1 ? (
              <div
                className="h-px w-full bg-op-card-border"
                aria-hidden
              />
            ) : null}
          </div>
        ))}
      </dl>
    </aside>
  )
}

/**
 * Campaign wizard Audience step — Figma 4695:51830.
 * Live Smart Group counts on non-deferred cards; eligibility summary is mock.
 */
export function CampaignAudienceStep({
  audience,
  onSelectAudience,
  onSelectSavedGroup,
}: CampaignAudienceStepProps) {
  const standardOptions = audience.options.filter(
    (option) => option.id !== "dormant-guests"
  )
  const dormantOption = audience.options.find(
    (option) => option.id === "dormant-guests"
  )

  return (
    <div className="flex w-full flex-col items-start justify-between gap-8 lg:flex-row">
      <div className="flex min-h-0 w-full max-w-[690px] flex-col gap-7">
        <header className="flex flex-col gap-2">
          <h2 className="m-0 text-xl font-semibold leading-normal text-op-text-primary sm:text-[22px]">
            {CAMPAIGN_AUDIENCE_COPY.stepHeading}
          </h2>
          <p className="m-0 text-sm font-medium leading-5 text-[var(--op-color-gray-550)]">
            {CAMPAIGN_AUDIENCE_COPY.stepDescription}
          </p>
        </header>

        <div
          className="flex min-h-0 flex-1 flex-col overflow-hidden rounded-[4px] border border-op-card-border bg-op-background-primary px-5"
          role="radiogroup"
          aria-label={CAMPAIGN_AUDIENCE_COPY.stepHeading}
        >
          <div className="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto pb-[22px] pt-3">
            {standardOptions.map((option) => (
              <div key={option.id} className="flex w-full flex-col gap-3">
                <AudienceOptionCard
                  option={option}
                  onSelect={() => {
                    onSelectAudience(option.id)
                  }}
                />
                {option.id === "saved-group" && audience.showSavedGroupPicker ? (
                  <div className="flex w-full flex-col gap-2">
                    <label
                      htmlFor="campaign-audience-saved-group"
                      className="text-sm font-semibold leading-5 text-op-text-primary"
                    >
                      {CAMPAIGN_AUDIENCE_COPY.chooseSavedGroupLabel}
                    </label>
                    <Select
                      value={audience.savedGroupId ?? undefined}
                      onValueChange={(value) => {
                        onSelectSavedGroup(value.length > 0 ? value : null)
                      }}
                    >
                      <SelectTrigger
                        id="campaign-audience-saved-group"
                        className="h-auto min-h-[50px] rounded-[4px] border-op-input-border bg-transparent px-[15px] py-[15px] text-sm data-placeholder:text-[var(--op-color-gray-550)]"
                      >
                        <SelectValue
                          placeholder={
                            CAMPAIGN_AUDIENCE_COPY.chooseSavedGroupPlaceholder
                          }
                        />
                      </SelectTrigger>
                      <SelectContent
                        position="popper"
                        className={CAMPAIGN_WIZARD_SELECT_MENU_CLASS}
                      >
                        {audience.savedGroupOptions.map((group) => (
                          <SelectItem key={group.value} value={group.value}>
                            {group.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                ) : null}
              </div>
            ))}

            {dormantOption ? (
              <>
                <div
                  className="h-px w-full bg-op-card-border"
                  aria-hidden
                />
                <AudienceOptionCard
                  option={dormantOption}
                  onSelect={() => {
                    onSelectAudience(dormantOption.id)
                  }}
                />
              </>
            ) : null}
          </div>
        </div>
      </div>

      <EligibilitySummary audience={audience} />
    </div>
  )
}
