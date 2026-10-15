import evidenceFeedbackImage from "@/assets/images/marketing/pilot/evidence-feedback.png"
import evidenceGuestContextImage from "@/assets/images/marketing/pilot/evidence-guest-context.png"
import {
  PILOT_EVIDENCE,
  PILOT_SECTION_BODY,
  PILOT_SECTION_HEADING,
  PILOT_SECTION_INSET,
  type PilotEvidenceRow,
} from "@/content/marketing/pilotPage"
import { cn } from "@/lib/utils"

const EVIDENCE_IMAGES = {
  feedback: evidenceFeedbackImage,
  "guest-context": evidenceGuestContextImage,
} as const

const mediaPlateClass =
  "aspect-[766/622] w-full overflow-hidden rounded-[12px] bg-[#e9e9e9] lg:aspect-auto lg:h-[622px] lg:w-[min(100%,766px)] lg:shrink-0"

function EvidenceMedia({
  row,
  className,
}: {
  row: PilotEvidenceRow
  className?: string
}) {
  if (row.media.kind === "placeholder") {
    return (
      <div
        className={cn(
          mediaPlateClass,
          "flex items-center justify-center",
          className,
        )}
        aria-hidden
      >
        <p className="m-0 px-6 text-center font-sans text-base font-semibold leading-normal text-black">
          {row.media.label}
        </p>
      </div>
    )
  }

  const src = EVIDENCE_IMAGES[row.id as "feedback" | "guest-context"]

  return (
    <div className={cn(mediaPlateClass, className)}>
      <img
        src={src}
        alt={row.media.imageAlt}
        width={766}
        height={622}
        className="size-full object-cover object-left-top"
        decoding="async"
      />
    </div>
  )
}

function EvidenceRow({ row }: { row: PilotEvidenceRow }) {
  const copyClass = cn(
    "flex w-full min-w-0 flex-1 flex-col items-start gap-[18px] lg:pt-[50px]",
    row.mediaFirst ? "order-1 lg:order-2 lg:pr-[70px]" : "lg:pl-[70px]",
  )
  const mediaClass = row.mediaFirst ? "order-2 lg:order-1" : undefined

  return (
    <div className="flex w-full flex-col gap-8 py-8 lg:flex-row lg:items-start lg:gap-[70px] lg:py-[30px]">
      <div className={copyClass}>
        <h3 className="m-0 max-w-[464px] font-jakarta text-[28px] font-medium leading-normal text-black lg:text-[36px]">
          {row.title}
        </h3>
        <p className={cn("m-0 max-w-[534px]", PILOT_SECTION_BODY)}>
          {row.body}
        </p>
      </div>
      <EvidenceMedia row={row} className={mediaClass} />
    </div>
  )
}

/**
 * Figma Pilot “Real activity” evidence strip `5661:20056`.
 * Feedback + Guest Context use plate images; Campaigns & Results stay gray.
 */
export function PilotEvidence() {
  return (
    <section
      className="w-full bg-white"
      aria-labelledby="pilot-evidence-heading"
    >
      <div
        className={cn(
          "flex w-full flex-col gap-[46px]",
          PILOT_SECTION_INSET,
          "pb-12.5 pt-12.5 lg:pb-[70px] lg:pt-[70px]",
        )}
      >
        <header className="flex w-full max-w-[615px] flex-col gap-[18px]">
          <p className="m-0 font-sans text-sm font-medium uppercase leading-normal text-black lg:text-base">
            {PILOT_EVIDENCE.eyebrow}
          </p>
          <h2
            id="pilot-evidence-heading"
            className={cn("m-0 text-black", PILOT_SECTION_HEADING)}
          >
            {PILOT_EVIDENCE.title}
          </h2>
          <p className={cn("m-0 max-w-[564px]", PILOT_SECTION_BODY)}>
            {PILOT_EVIDENCE.body}
          </p>
        </header>

        <div className="flex w-full flex-col">
          {PILOT_EVIDENCE.rows.map((row) => (
            <EvidenceRow key={row.id} row={row} />
          ))}
        </div>
      </div>
    </section>
  )
}
