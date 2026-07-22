import { GuestProfileDetailRows } from "@/components/dashboard/operator/GuestProfile/GuestProfileDetailRows"
import { GuestProfileSectionEmptyCard } from "@/components/dashboard/operator/GuestProfile/GuestProfileSectionEmptyCard"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { GUEST_PROFILE_EMPTY_COPY } from "@/lib/operatorGuestProfile/guestProfilePresentation"
import {
  GUESTS_MARKETING_STATUS_BADGE_CLASS,
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_HEADER_ROW_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
  GUESTS_TABLE_BODY_CELL_CLASS,
  GUESTS_TABLE_BODY_ROW_CLASS,
  GUESTS_TABLE_CLASS,
  GUESTS_TABLE_FRAME_CLASS,
  GUESTS_TABLE_GUEST_NAME_CLASS,
  GUESTS_TABLE_HEAD_CELL_CLASS,
  GUESTS_TABLE_HEAD_ROW_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import type { OperatorGuestProfileViewModel } from "@/types/operatorGuestProfile"

type GuestProfileOverviewPanelProps = {
  viewModel: OperatorGuestProfileViewModel
}

const OVERVIEW_DETAIL_ROWS: Array<{
  label: string
  value: (vm: OperatorGuestProfileViewModel) => string | number
}> = [
  {
    label: "Guest since",
    value: (vm) => vm.overviewDetails.guestSinceDisplay,
  },
  {
    label: "Offers claimed",
    value: (vm) => vm.overviewDetails.offersClaimed,
  },
  {
    label: "Total interactions",
    value: (vm) => vm.overviewDetails.totalInteractions,
  },
  {
    label: "Campaigns sent",
    value: (vm) => vm.overviewDetails.campaignsSent,
  },
  {
    label: "Feedback received",
    value: (vm) => vm.overviewDetails.feedbackReceived,
  },
  {
    label: "Last activity",
    value: (vm) => vm.overviewDetails.lastActivityDisplay,
  },
]

function DisabledAddNoteButton() {
  return (
    <Button
      type="button"
      disabled
      aria-disabled
      aria-label="Add note (unavailable)"
      title="Add note is unavailable"
      className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
    >
      Add note
    </Button>
  )
}

export function GuestProfileOverviewPanel({
  viewModel,
}: GuestProfileOverviewPanelProps) {
  const feedback = GUEST_PROFILE_EMPTY_COPY.overviewLatestFeedback
  const offer = GUEST_PROFILE_EMPTY_COPY.overviewLatestOffer
  const campaign = GUEST_PROFILE_EMPTY_COPY.overviewLatestCampaign
  const notes = GUEST_PROFILE_EMPTY_COPY.overviewRecentNotes

  return (
    <div className="flex flex-col gap-5">
      <section
        aria-label="Overview details"
        className={GUESTS_SECTION_CLASS}
      >
        <GuestProfileDetailRows
          rows={OVERVIEW_DETAIL_ROWS.map((row) => ({
            label: row.label,
            value: row.value(viewModel),
          }))}
        />
      </section>

      <section className={GUESTS_SECTION_CLASS}>
        <div className={GUESTS_SECTION_HEADER_ROW_CLASS}>
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>Contact eligibility</h2>
        </div>
        <div className={GUESTS_TABLE_FRAME_CLASS}>
          <Table className={GUESTS_TABLE_CLASS}>
            <TableHeader className="[&_tr]:border-0">
              <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Channel
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Status
                </TableHead>
                <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  Detail
                </TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {viewModel.contactEligibility.map((row) => (
                <TableRow
                  key={row.channel}
                  className={GUESTS_TABLE_BODY_ROW_CLASS}
                >
                  <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                    <span className={GUESTS_TABLE_GUEST_NAME_CLASS}>
                      {row.channelLabel}
                    </span>
                  </TableCell>
                  <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                    <Badge
                      variant="soft"
                      className={GUESTS_MARKETING_STATUS_BADGE_CLASS}
                    >
                      {row.statusLabel}
                    </Badge>
                  </TableCell>
                  <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS} />
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      </section>

      <GuestProfileSectionEmptyCard
        sectionTitle={feedback.sectionTitle}
        emptyTitle={feedback.emptyTitle}
        emptyHelper={feedback.emptyHelper}
      />
      <GuestProfileSectionEmptyCard
        sectionTitle={offer.sectionTitle}
        emptyTitle={offer.emptyTitle}
        emptyHelper={offer.emptyHelper}
      />
      <GuestProfileSectionEmptyCard
        sectionTitle={campaign.sectionTitle}
        emptyTitle={campaign.emptyTitle}
        emptyHelper={campaign.emptyHelper}
      />
      <GuestProfileSectionEmptyCard
        sectionTitle={notes.sectionTitle}
        emptyTitle={notes.emptyTitle}
        emptyHelper={notes.emptyHelper}
        emptyFooter={<DisabledAddNoteButton />}
      />
    </div>
  )
}
