import { Badge } from "@/components/ui/badge"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  GUESTS_MARKETING_STATUS_BADGE_CLASS,
  GUESTS_SECTION_SUBTITLE_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
  GUESTS_TABLE_BODY_CELL_CLASS,
  GUESTS_TABLE_BODY_ROW_CLASS,
  GUESTS_TABLE_CLASS,
  GUESTS_TABLE_FRAME_CLASS,
  GUESTS_TABLE_GUEST_NAME_CLASS,
  GUESTS_TABLE_HEAD_CELL_CLASS,
  GUESTS_TABLE_HEAD_ROW_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import {
  PRIVACY_CONSENT_CARD_CLASS,
  PRIVACY_CONSENT_PAGE_COPY,
  type PrivacySetupStatusRow,
} from "@/lib/operatorPrivacyConsent/privacyConsentPresentation"
import { cn } from "@/lib/utils"

type PrivacySetupStatusSectionProps = {
  rows: readonly PrivacySetupStatusRow[]
}

/** Privacy setup status card — Figma 3853:27279. */
export function PrivacySetupStatusSection({
  rows,
}: PrivacySetupStatusSectionProps) {
  const copy = PRIVACY_CONSENT_PAGE_COPY

  return (
    <section
      className={cn(PRIVACY_CONSENT_CARD_CLASS, "gap-10")}
      aria-label={copy.privacySetupStatusTitle}
    >
      <header className="flex flex-col gap-2 leading-[0]">
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>
          {copy.privacySetupStatusTitle}
        </h2>
        <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
          {copy.privacySetupStatusSubtitle}
        </p>
      </header>

      <div className={GUESTS_TABLE_FRAME_CLASS}>
        <Table className={GUESTS_TABLE_CLASS}>
          <TableHeader className="[&_tr]:border-0">
            <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
              <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                {copy.requirementColumn}
              </TableHead>
              <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                {copy.statusColumn}
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {rows.map((row) => (
              <TableRow key={row.id} className={GUESTS_TABLE_BODY_ROW_CLASS}>
                <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                  <span className={GUESTS_TABLE_GUEST_NAME_CLASS}>
                    {row.requirement}
                  </span>
                </TableCell>
                <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                  <Badge
                    variant="soft"
                    className={GUESTS_MARKETING_STATUS_BADGE_CLASS}
                  >
                    {row.status}
                  </Badge>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </section>
  )
}
