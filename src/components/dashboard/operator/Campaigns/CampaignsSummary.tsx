import {
  GUESTS_KPI_CARD_CLASS,
  GUESTS_KPI_DESCRIPTION_CLASS,
  GUESTS_KPI_GRID_CLASS,
  GUESTS_KPI_LABEL_CLASS,
  GUESTS_KPI_VALUE_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_SUBTITLE_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import type { OperatorCampaignsSummaryViewModel } from "@/lib/operatorCampaigns/createOperatorCampaignsPageModule"

type CampaignsSummaryProps = {
  summary: OperatorCampaignsSummaryViewModel
}

/** Campaign summary KPI strip — live Marketing eligible + fixed sibling mocks. */
export function CampaignsSummary({ summary }: CampaignsSummaryProps) {
  return (
    <section className={GUESTS_SECTION_CLASS} aria-label={summary.title}>
      <header className="flex flex-col gap-2 leading-[0]">
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>{summary.title}</h2>
        <p className={GUESTS_SECTION_SUBTITLE_CLASS}>{summary.subtitle}</p>
      </header>

      <div className={GUESTS_KPI_GRID_CLASS}>
        {summary.kpis.map((kpi) => (
          <div key={kpi.id} className={GUESTS_KPI_CARD_CLASS}>
            <div className="flex flex-col gap-0.5">
              <p className={GUESTS_KPI_LABEL_CLASS}>{kpi.label}</p>
              <p className={GUESTS_KPI_VALUE_CLASS}>
                {kpi.id === "messages-sent"
                  ? kpi.value.toLocaleString("en-GB")
                  : (kpi.value ?? 0)}
              </p>
            </div>
            <p className={GUESTS_KPI_DESCRIPTION_CLASS}>{kpi.description}</p>
          </div>
        ))}
      </div>
    </section>
  )
}
