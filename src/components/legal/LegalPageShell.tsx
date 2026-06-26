import {
  useCallback,
  useEffect,
  useState,
  type MouseEvent,
} from "react"

import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion"
import { Separator } from "@/components/ui/separator"
import { cn } from "@/lib/utils"

import type { LegalPageContent, LegalSection } from "@/content/legal/types"

const NAVBAR_SCROLL_OFFSET_PX = 88

const tocLinkClass =
  "rounded-sm text-left text-base font-medium leading-6 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/30"

type LegalPageShellProps = {
  content: LegalPageContent
}

function scrollToSection(sectionId: string) {
  const prefersReducedMotion = window.matchMedia(
    "(prefers-reduced-motion: reduce)"
  ).matches

  const target = document.getElementById(sectionId)
  if (!target) {
    return
  }

  const top =
    target.getBoundingClientRect().top +
    window.scrollY -
    NAVBAR_SCROLL_OFFSET_PX

  window.scrollTo({
    top,
    behavior: prefersReducedMotion ? "auto" : "smooth",
  })
}

function handleTocClick(
  event: MouseEvent<HTMLAnchorElement>,
  sectionId: string,
  onNavigate?: () => void
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
    <ul className={cn("m-0 flex list-none flex-col gap-3 p-0", className)}>
      {sections.map((section) => {
        const isActive = section.id === activeSectionId

        return (
          <li key={section.id}>
            <a
              href={`#${section.id}`}
              onClick={(event) => handleTocClick(event, section.id, onNavigate)}
              className={cn(
                tocLinkClass,
                "block break-words",
                isActive ? "text-[#141414]" : "text-[#a7a7a7] hover:text-[#141414]"
              )}
              aria-current={isActive ? "location" : undefined}
            >
              {section.title}
            </a>
          </li>
        )
      })}
    </ul>
  )
}

export function LegalPageShell({ content }: LegalPageShellProps) {
  const { title, description, sections } = content
  const [activeSectionId, setActiveSectionId] = useState(sections[0]?.id ?? "")
  const [mobileTocValue, setMobileTocValue] = useState<string | undefined>(
    undefined
  )

  const closeMobileToc = useCallback(() => {
    setMobileTocValue(undefined)
  }, [])

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
          (a, b) => a.boundingClientRect.top - b.boundingClientRect.top
        )[0]?.target.id

        if (nextActive) {
          setActiveSectionId(nextActive)
        }
      },
      {
        rootMargin: `-${NAVBAR_SCROLL_OFFSET_PX}px 0px -55% 0px`,
        threshold: [0, 0.1, 0.5, 1],
      }
    )

    for (const element of elements) {
      observer.observe(element)
    }

    return () => observer.disconnect()
  }, [sections])

  return (
    <main className="w-full bg-white text-[#141414]">
      <div className="mx-auto flex w-full max-w-7xl flex-col gap-10 px-4 py-10 sm:px-6 sm:py-12 md:px-10 lg:gap-15 lg:px-16 lg:py-16 xl:px-20 2xl:max-w-[108rem] 2xl:px-45">
        <header className="flex max-w-3xl flex-col gap-5 sm:gap-5.5">
          <h1 className="m-0 text-[clamp(2rem,5vw,2.875rem)] font-bold leading-normal text-[#141414]">
            {title}
          </h1>
          <p className="m-0 text-base font-medium leading-6 text-[#141414] sm:text-lg sm:leading-6">
            {description}
          </p>
        </header>

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

        <div className="flex flex-col gap-10 lg:flex-row lg:items-start lg:gap-16 xl:gap-24 2xl:gap-36">
          <nav
            aria-label="Table of contents"
            className="hidden lg:sticky lg:top-[5.5rem] lg:block lg:max-w-[min(100%,17rem)] lg:shrink-0 xl:max-w-xs"
          >
            <TableOfContentsLinks
              sections={sections}
              activeSectionId={activeSectionId}
            />
          </nav>

          <article className="min-w-0 flex-1">
            <div className="flex flex-col gap-8">
              {sections.map((section, index) => (
                <section
                  key={section.id}
                  id={section.id}
                  className="scroll-mt-[5.5rem]"
                >
                  <div className="flex flex-col gap-5 sm:gap-5.5">
                    <h2 className="m-0 text-[clamp(1.375rem,2.5vw,1.625rem)] font-bold leading-normal text-[#141414]">
                      {section.title}
                    </h2>
                    {section.content ? (
                      <div className="text-base leading-[1.375rem] text-[#141414] [&_li]:ms-6 [&_li]:list-item [&_p+p]:mt-0 [&_p]:m-0 [&_ul]:m-0 [&_ul]:list-disc [&_ul]:ps-6">
                        {section.content}
                      </div>
                    ) : null}
                  </div>
                  {index < sections.length - 1 ? (
                    <Separator className="mt-8 bg-[#e7e7e7]" />
                  ) : null}
                </section>
              ))}
            </div>
          </article>
        </div>
      </div>
    </main>
  )
}
