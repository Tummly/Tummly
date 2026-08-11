import { OfferDetailsLifecycleEmptyState } from "@/components/dashboard/operator/Offers/OfferDetailsLifecycleEmptyState"
import { Button } from "@/components/ui/button"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import type { OfferDetailsCampaignsTabViewModel } from "@/lib/operatorOffers/createOfferDetailsPageModule"
import type { OfferDetailsCampaignsSubTabId } from "@/lib/operatorOffers/offerDetailsPresentation"
import {
  CAPTURE_PLACEMENTS_BODY_CELL_CLASS,
  CAPTURE_PLACEMENTS_BODY_ROW_CLASS,
  CAPTURE_PLACEMENTS_HEAD_ACTIONS_CELL_CLASS,
  CAPTURE_PLACEMENTS_HEAD_CELL_CLASS,
  CAPTURE_PLACEMENTS_HEAD_ROW_CLASS,
  CAPTURE_PLACEMENTS_NAME_CELL_CLASS,
  CAPTURE_PLACEMENTS_TABLE_CLASS,
  CAPTURE_PLACEMENTS_TABLE_FRAME_CLASS,
} from "@/lib/operatorCapture/capturePresentation"
import {
  GUESTS_TAB_BUTTON_ACTIVE_CLASS,
  GUESTS_TAB_BUTTON_CLASS,
  GUESTS_TAB_BUTTON_INACTIVE_CLASS,
  GUESTS_TABLIST_CLASS,
  GUESTS_TABLIST_SCROLL_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import { cn } from "@/lib/utils"

type OfferDetailsCampaignsPanelProps = {
  campaigns: OfferDetailsCampaignsTabViewModel
  onSubTabChange: (subTabId: OfferDetailsCampaignsSubTabId) => void
}

export function OfferDetailsCampaignsPanel({
  campaigns,
  onSubTabChange,
}: OfferDetailsCampaignsPanelProps) {
  const active =
    campaigns.activeSubTabId === "linked"
      ? campaigns.linked
      : campaigns.issuanceSources

  return (
    <div className="flex flex-col gap-4">
      <div className={GUESTS_TABLIST_SCROLL_CLASS}>
        <div
          className={GUESTS_TABLIST_CLASS}
          role="tablist"
          aria-label="Campaigns sub-tabs"
        >
          {campaigns.subTabs.map((tab) => {
            const selected = tab.id === campaigns.activeSubTabId
            return (
              <Button
                key={tab.id}
                type="button"
                role="tab"
                aria-selected={selected}
                variant="op-ghost"
                className={cn(
                  GUESTS_TAB_BUTTON_CLASS,
                  selected
                    ? GUESTS_TAB_BUTTON_ACTIVE_CLASS
                    : GUESTS_TAB_BUTTON_INACTIVE_CLASS
                )}
                onClick={() => {
                  onSubTabChange(tab.id)
                }}
              >
                {tab.label}
              </Button>
            )
          })}
        </div>
      </div>

      {active.empty != null ? (
        <OfferDetailsLifecycleEmptyState empty={active.empty} />
      ) : campaigns.activeSubTabId === "linked" ? (
        <div className={CAPTURE_PLACEMENTS_TABLE_FRAME_CLASS}>
          <Table className={CAPTURE_PLACEMENTS_TABLE_CLASS}>
            <TableHeader>
              <TableRow className={CAPTURE_PLACEMENTS_HEAD_ROW_CLASS}>
                <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
                  {campaigns.linked.columns.campaign}
                </TableHead>
                <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
                  {campaigns.linked.columns.status}
                </TableHead>
                <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
                  {campaigns.linked.columns.location}
                </TableHead>
                <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
                  {campaigns.linked.columns.channel}
                </TableHead>
                <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
                  {campaigns.linked.columns.audience}
                </TableHead>
                <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
                  {campaigns.linked.columns.offerVersion}
                </TableHead>
                <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
                  {campaigns.linked.columns.passesIssued}
                </TableHead>
                <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
                  {campaigns.linked.columns.claims}
                </TableHead>
                <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
                  {campaigns.linked.columns.redemptions}
                </TableHead>
                <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
                  {campaigns.linked.columns.sendDate}
                </TableHead>
                <TableHead className={CAPTURE_PLACEMENTS_HEAD_ACTIONS_CELL_CLASS}>
                  {campaigns.linked.columns.actions}
                </TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {campaigns.linked.rows.map((row) => (
                <TableRow
                  key={row.id}
                  className={CAPTURE_PLACEMENTS_BODY_ROW_CLASS}
                >
                  <TableCell className={CAPTURE_PLACEMENTS_NAME_CELL_CLASS}>
                    {row.campaignName}
                  </TableCell>
                  <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                    {row.statusText}
                  </TableCell>
                  <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                    {row.locationName}
                  </TableCell>
                  <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                    {row.channelText}
                  </TableCell>
                  <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                    {row.audienceText}
                  </TableCell>
                  <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                    {row.offerVersionText}
                  </TableCell>
                  <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                    {row.passesIssuedText}
                  </TableCell>
                  <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                    {row.claimsText}
                  </TableCell>
                  <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                    {row.redemptionsText}
                  </TableCell>
                  <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                    {row.sendDateText}
                  </TableCell>
                  <TableCell className={CAPTURE_PLACEMENTS_HEAD_ACTIONS_CELL_CLASS} />
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      ) : (
        <div className={CAPTURE_PLACEMENTS_TABLE_FRAME_CLASS}>
          <Table className={CAPTURE_PLACEMENTS_TABLE_CLASS}>
            <TableHeader>
              <TableRow className={CAPTURE_PLACEMENTS_HEAD_ROW_CLASS}>
                <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
                  {campaigns.issuanceSources.columns.source}
                </TableHead>
                <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
                  {campaigns.issuanceSources.columns.path}
                </TableHead>
                <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
                  {campaigns.issuanceSources.columns.passesIssued}
                </TableHead>
                <TableHead className={CAPTURE_PLACEMENTS_HEAD_CELL_CLASS}>
                  {campaigns.issuanceSources.columns.lastIssued}
                </TableHead>
                <TableHead className={CAPTURE_PLACEMENTS_HEAD_ACTIONS_CELL_CLASS}>
                  {campaigns.issuanceSources.columns.actions}
                </TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {campaigns.issuanceSources.rows.map((row) => (
                <TableRow
                  key={row.id}
                  className={CAPTURE_PLACEMENTS_BODY_ROW_CLASS}
                >
                  <TableCell className={CAPTURE_PLACEMENTS_NAME_CELL_CLASS}>
                    {row.sourceText}
                  </TableCell>
                  <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                    {row.pathText}
                  </TableCell>
                  <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                    {row.passesIssuedText}
                  </TableCell>
                  <TableCell className={CAPTURE_PLACEMENTS_BODY_CELL_CLASS}>
                    {row.lastIssuedText}
                  </TableCell>
                  <TableCell className={CAPTURE_PLACEMENTS_HEAD_ACTIONS_CELL_CLASS} />
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  )
}
