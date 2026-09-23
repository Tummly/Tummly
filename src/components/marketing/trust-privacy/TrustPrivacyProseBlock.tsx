import type { TrustPrivacyProseSection } from "@/content/marketing/trustPrivacyPage"
import { marketingChromeContentInset } from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

const sectionHeading =
  "font-serif text-[28px] font-medium leading-normal text-[#141414] lg:text-[36px]"

const sectionBody =
  "text-base font-normal leading-[22px] text-[#141414] lg:text-[18px] lg:leading-6"

type TrustPrivacyProseSectionProps = {
  section: TrustPrivacyProseSection
}

/** Figma prose blocks (`4974:26905` … `4974:26930`). */
export function TrustPrivacyProseBlock({
  section,
}: TrustPrivacyProseSectionProps) {
  return (
    <section
      className={cn(
        "w-full",
        section.mutedBackground ? "bg-[#f0f0f0]" : "bg-white",
      )}
    >
      <div
        className={cn(
          "mx-auto flex w-full flex-col gap-10",
          marketingChromeContentInset,
          "py-17.5",
        )}
      >
        <h2 className={cn("m-0 max-w-170", sectionHeading)}>{section.title}</h2>

        <div className={cn("max-w-224.5", sectionBody)}>
          {section.paragraphs.map((paragraph, index) => {
            const isLastParagraph = index === section.paragraphs.length - 1
            const hasMore =
              Boolean(section.bullets?.length)
              || Boolean(section.afterBullets?.length)
              || !isLastParagraph

            return (
              <p
                key={paragraph}
                className={cn("m-0", hasMore && "mb-6")}
              >
                {paragraph}
              </p>
            )
          })}

          {section.bullets ? (
            <ul
              className={cn(
                "m-0 list-disc space-y-0 pl-7",
                section.afterBullets?.length ? "mb-6" : undefined,
              )}
            >
              {section.bullets.map((bullet) => (
                <li key={bullet} className="leading-6">
                  {bullet}
                </li>
              ))}
            </ul>
          ) : null}

          {section.afterBullets?.map((paragraph, index) => (
            <p
              key={paragraph}
              className={cn(
                "m-0",
                index < (section.afterBullets?.length ?? 0) - 1 && "mb-6",
              )}
            >
              {paragraph}
            </p>
          ))}
        </div>
      </div>
    </section>
  )
}
