import {
  useCallback,
  useEffect,
  useState,
  type MouseEvent,
} from "react"
import { DownloadIcon } from "lucide-react"

import { downloadLegalDocument } from "@/api/legalDocumentsApi"
import { CookieSettingsTrigger } from "@/components/common/CookieSettingsDialog"
import { MarketingSignUpCta } from "@/components/marketing/MarketingSignUpCta"
import { LegalRelatedLinks } from "@/components/legal/LegalRelatedLinks"
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion"
import { Button } from "@/components/ui/button"
import { LEGAL_SIGN_UP_CTA } from "@/content/legal/legalSignUpCta"
import type { LegalPageContent, LegalSection } from "@/content/legal/types"
import { marketingChromeContentInset } from "@/lib/marketing-layout"
import { warmCtaLaunchBg } from "@/lib/prefetchCtaLaunchBg"
import { cn } from "@/lib/utils"

import marketingArrowRight from "@/assets/svg/marketing-arrow-right.svg"

/**
 * Clear the sticky marketing header (chrome pad + banner + gap + nav).
 * Too small a value parks the TOC under the header so the top items look cut off.
 */
const MARKETING_HEADER_STICKY_OFFSET_PX = 180
const stickyTopClass = "lg:top-[180px]"
const stickyMaxHeightClass = "lg:max-h-[calc(100vh-12.5rem)]"
const sectionScrollMarginClass = "scroll-mt-[180px]"

const heroHeading =
  "font-jakarta text-[36px] font-medium leading-10 tracking-normal lg:text-[66px] lg:leading-[74px]"

const heroMeta =
  "text-base font-medium leading-[22px] tracking-normal lg:text-[18px] lg:leading-6"

const heroBody =
  "text-base font-normal leading-[22px] tracking-normal lg:text-[18px] lg:leading-6"

const sectionHeading =
  "text-[24px] font-medium leading-normal text-[#141414] lg:text-[28px]"

const sectionBody =
  "flex flex-col gap-3 text-base leading-[22px] text-[#141414] lg:text-[18px] lg:leading-6 [&_h4]:m-0 [&_h4]:pt-3 [&_h4]:text-base [&_h4]:font-semibold [&_h4]:leading-6 [&_h4:first-child]:pt-0 [&_li]:ms-6 [&_li]:list-item [&_p]:m-0 [&_ul]:m-0 [&_ul]:list-disc [&_ul]:ps-6"

const tocLinkClass =
  "rounded-sm text-left text-base font-normal leading-[26px] transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/30"

const manageCookiesClass =
  "inline-flex items-center gap-2 rounded-[4px] text-sm font-medium leading-5 text-[#141414] no-underline transition-colors hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/30"

type LegalPageShellProps = {
  content: LegalPageContent
}

function descriptionParagraphs(
  description: LegalPageContent["description"],
): string[] {
  return typeof description === "string" ? [description] : [...description]
}

/** TOC uses a decimal list; strip a leading “N. ” from section titles. */
function tocLabel(title: string): string {
  return title.replace(/^\d+\.\s*/, "").trim()
}

function scrollToSection(sectionId: string) {
  const prefersReducedMotion = window.matchMedia(
    "(prefers-reduced-motion: reduce)",
  ).matches

  const target = document.getElementById(sectionId)
  if (!target) {
    return
  }

  const top =
    target.getBoundingClientRect().top
    + window.scrollY
    - MARKETING_HEADER_STICKY_OFFSET_PX

  window.scrollTo({
    top,
    behavior: prefersReducedMotion ? "auto" : "smooth",
  })
}

function handleTocClick(
  event: MouseEvent<HTMLAnchorElement>,
  sectionId: string,
  onNavigate?: () => void,
) {
  event.preventDefault()
  scrollToSection(sectionId)
  onNavigate?.()
}

function TableOfContentsLinks({
  sections,
  activeSectionId,
  onNavigate,
  className,
}: {
  sections: LegalSection[]
  activeSectionId: string
  onNavigate?: () => void
  className?: string
}) {
  return (
    <ol
      className={cn(
        "m-0 flex list-decimal flex-col gap-0 p-0 ps-7",
        className,
      )}
    >
      {sections.map((section) => {
        const isActive = section.id === activeSectionId

        return (
          <li key={section.id} className="marker:text-[#141414]">
            <a
              href={`#${section.id}`}
              onClick={(event) => handleTocClick(event, section.id, onNavigate)}
              className={cn(
                tocLinkClass,
                "block break-words",
                isActive
                  ? "text-[#141414]"
                  : "text-[#141414]/70 hover:text-[#141414]",
              )}
              aria-current={isActive ? "location" : undefined}
            >
              {tocLabel(section.title)}
            </a>
          </li>
        )
      })}
    </ol>
  )
}

/**
 * Shared legal document layout (Figma Privacy Notice `4974:26950` and siblings).
 */
export function LegalPageShell({ content }: LegalPageShellProps) {
  const {
    title,
    description,
    lastUpdated,
    showManageCookiePreferences,
    documentKey,
    sections,
  } = content
  const paragraphs = descriptionParagraphs(description)
  const [activeSectionId, setActiveSectionId] = useState(sections[0]?.id ?? "")
  const [mobileTocValue, setMobileTocValue] = useState<string | undefined>(
    undefined,
  )
  const [isDownloading, setIsDownloading] = useState(false)
  const [downloadError, setDownloadError] = useState<string | null>(null)

  useEffect(() => {
    warmCtaLaunchBg({ priority: true })
  }, [])

  const closeMobileToc = useCallback(() => {
    setMobileTocValue(undefined)
  }, [])

  const handleDownload = useCallback(async () => {
    if (!documentKey) {
      return
    }

    setDownloadError(null)
    setIsDownloading(true)
    try {
      await downloadLegalDocument(documentKey)
    } catch (error: unknown) {
      setDownloadError(
        error instanceof Error
          ? error.message
          : "Unable to download the legal document. Please try again.",
      )
    } finally {
      setIsDownloading(false)
    }
  }, [documentKey])

  useEffect(() => {
    const elements = sections
      .map((section) => document.getElementById(section.id))
      .filter((element): element is HTMLElement => element != null)

    if (elements.length === 0) {
      return
    }

    const visibleSections = new Map<string, IntersectionObserverEntry>()

    const observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          const id = entry.target.id
          if (entry.isIntersecting) {
            visibleSections.set(id, entry)
          } else {
            visibleSections.delete(id)
          }
        }

        if (visibleSections.size === 0) {
          return
        }

        const nextActive = [...visibleSections.values()].sort(
          (a, b) => a.boundingClientRect.top - b.boundingClientRect.top,
        )[0]?.target.id

        if (nextActive) {
          setActiveSectionId(nextActive)
        }
      },
      {
        rootMargin: `-${MARKETING_HEADER_STICKY_OFFSET_PX}px 0px -55% 0px`,
        threshold: [0, 0.1, 0.5, 1],
      },
    )

    for (const element of elements) {
      observer.observe(element)
    }

    return () => observer.disconnect()
  }, [sections])

  return (
    <>
      <main className="w-full bg-white text-[#141414]">
        <header className="-mt-5 w-full bg-[#fafafa]">
          <div
            className={cn(
              "mx-auto flex w-full flex-col gap-4.5",
              marketingChromeContentInset,
              "py-17.5",
            )}
          >
            <div className="flex max-w-214.25 flex-col gap-4.5">
              <h1 className={cn("m-0 max-w-214 text-[#141414]", heroHeading)}>
                {title}
              </h1>

              {lastUpdated ? (
                <p className={cn("m-0 max-w-168.5 text-[#141414]", heroMeta)}>
                  Last updated: {lastUpdated}
                </p>
              ) : null}

              <div
                className={cn(
                  "flex max-w-198 flex-col gap-6 text-[#141414]",
                  heroBody,
                )}
              >
                {paragraphs.map((paragraph) => (
                  <p key={paragraph} className="m-0">
                    {paragraph}
                  </p>
                ))}
              </div>
            </div>

            {showManageCookiePreferences ? (
              <CookieSettingsTrigger className={manageCookiesClass}>
                Manage cookie preferences
                <img
                  src={marketingArrowRight}
                  alt=""
                  width={15}
                  height={10}
                  className="block size-auto h-2.5 w-3.75 shrink-0 brightness-0"
                  aria-hidden
                />
              </CookieSettingsTrigger>
            ) : null}

            {documentKey ? (
              <div className="flex flex-col items-start gap-2">
                <Button
                  type="button"
                  variant="outline"
                  size="responsive"
                  onClick={handleDownload}
                  disabled={isDownloading}
                >
                  <DownloadIcon data-icon="inline-start" />
                  {isDownloading ? "Downloading…" : "Download document"}
                </Button>
                {downloadError ? (
                  <p role="alert" className="m-0 text-sm text-destructive">
                    {downloadError}
                  </p>
                ) : null}
              </div>
            ) : null}
          </div>
        </header>

        <div
          className={cn(
            "mx-auto flex w-full flex-col gap-10",
            marketingChromeContentInset,
            "pb-17.5 pt-12.5 lg:gap-15",
          )}
        >
          <div className="lg:hidden">
            <Accordion
              type="single"
              collapsible
              value={mobileTocValue}
              onValueChange={setMobileTocValue}
            >
              <AccordionItem value="on-this-page" className="border-[#e7e7e7]">
                <AccordionTrigger className="py-3 text-base font-medium text-[#141414] hover:no-underline">
                  On this page
                </AccordionTrigger>
                <AccordionContent className="pb-4">
                  <nav aria-label="On this page">
                    <TableOfContentsLinks
                      sections={sections}
                      activeSectionId={activeSectionId}
                      onNavigate={closeMobileToc}
                    />
                  </nav>
                </AccordionContent>
              </AccordionItem>
            </Accordion>
          </div>

          <div className="flex flex-col gap-10 lg:flex-row lg:items-start lg:gap-15">
            <nav
              aria-label="Table of contents"
              className={cn(
                "hidden lg:sticky lg:block lg:w-[min(100%,34.25rem)] lg:shrink-0 lg:overflow-y-auto lg:overscroll-contain lg:pt-5 lg:scrollbar-none",
                stickyTopClass,
                stickyMaxHeightClass,
              )}
            >
              <TableOfContentsLinks
                sections={sections}
                activeSectionId={activeSectionId}
              />
            </nav>

            <article className="min-w-0 flex-1 pt-5">
              <div className="flex flex-col gap-15">
                {sections.map((section) => (
                  <section
                    key={section.id}
                    id={section.id}
                    className={sectionScrollMarginClass}
                  >
                    <div className="flex flex-col gap-5">
                      <h2 className={cn("m-0 max-w-170", sectionHeading)}>
                        {section.title}
                      </h2>
                      {section.content ? (
                        <div className={cn("max-w-200", sectionBody)}>
                          {section.content}
                        </div>
                      ) : null}
                    </div>
                  </section>
                ))}
              </div>
            </article>
          </div>
        </div>
      </main>

      <LegalRelatedLinks />
      <MarketingSignUpCta content={LEGAL_SIGN_UP_CTA} />
    </>
  )
}
