import { readdirSync, readFileSync } from "node:fs"
import { join, relative } from "node:path"
import { fileURLToPath } from "node:url"
import { describe, expect, it } from "vitest"

import { HELP_CENTRE_ARTICLES } from "@/content/helpCentre/articles"
import { FAQS_PAGE_SECTIONS } from "@/content/marketing/faqsPage"
import { PILOT_FAQS } from "@/content/marketing/pilotPage"
import { PRICING_FAQS } from "@/content/marketing/pricingPage"
import { buildManagePlanFaqItems } from "@/lib/operatorBillingCredits/managePlanPresentation"

const PILOT_START_ANSWER =
  "When you successfully complete setup for the Pilot account you selected. Your setup confirmation shows the start and end dates. No QR scan or physical delivery is needed."

const PILOT_END_ANSWER =
  "There is no automatic charge or paid renewal. Live features pause when the Pilot ends. Choose a paid plan to continue, with account history retained under the applicable retention rules."

const PILOT_TERMS_LINE =
  "No payment card. No automatic paid renewal. Pilot allowances are once, not monthly."

const SRC_DIR = fileURLToPath(new URL("../..", import.meta.url))

const FORBIDDEN_PILOT_COPY = [
  "first real production guest scan",
  "first valid production guest scan",
  "confirmed delivered",
  "redeem the Activation Code",
]

function listSourceFiles(dir: string): string[] {
  return readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const full = join(dir, entry.name)
    if (entry.isDirectory()) return listSourceFiles(full)
    if (!/\.(ts|tsx)$/.test(entry.name)) return []
    if (/\.(test|spec)\.(ts|tsx)$/.test(entry.name)) return []
    return [full]
  })
}

function pricingFaqAnswer(id: string): string | undefined {
  return PRICING_FAQS.items.find((item) => item.id === id)?.answer
}

function faqsPageAnswer(id: string): string | undefined {
  for (const section of FAQS_PAGE_SECTIONS) {
    const found = section.items.find((item) => item.id === id)
    if (found) return found.answer
  }
  return undefined
}

describe("Pilot copy follows doc 08", () => {
  it("pricing pilot-start answer is the N10.19 string", () => {
    expect(pricingFaqAnswer("pilot-start")).toBe(PILOT_START_ANSWER)
  })

  it("FAQ when-pilot-starts answer is the N10.19 string", () => {
    expect(faqsPageAnswer("when-pilot-starts")).toBe(PILOT_START_ANSWER)
  })

  it("Pilot page when-begins answer is the N10.19 string", () => {
    expect(PILOT_FAQS.items.find((item) => item.id === "when-begins")?.answer).toBe(
      PILOT_START_ANSWER
    )
  })

  it("pricing pilot-end answer is the N10.05 string", () => {
    expect(pricingFaqAnswer("pilot-end")).toBe(PILOT_END_ANSWER)
  })

  it("FAQ when-pilot-ends answer is the N10.05 string", () => {
    expect(faqsPageAnswer("when-pilot-ends")).toBe(PILOT_END_ANSWER)
  })

  it("Manage Plan pilot-start paragraphs are the N10.19 string and the N02.46 terms line", () => {
    const item = buildManagePlanFaqItems({ vatRateBps: 0 }).find(
      (row) => row.id === "pilot-start"
    )
    expect(item?.answerParagraphs).toEqual([PILOT_START_ANSWER, PILOT_TERMS_LINE])
  })

  it("help article activation-code keeps its title and does not claim the code unlocks or starts the Pilot", () => {
    const article = HELP_CENTRE_ARTICLES.find(
      (row) => row.slug === "activation-code"
    )
    expect(article?.title).toBe("Account activation explained")
    expect(article?.body).toContain(PILOT_START_ANSWER)
    expect(article?.body).toMatch(/does not start the Pilot/)
    expect(article?.body).toMatch(/14 or 30 days/)
    expect(article?.body).not.toMatch(/unlock your/i)
    expect(article?.body).not.toMatch(/code (will )?starts? the Pilot/i)
  })

  it("no source file ships the retired Pilot start wording", () => {
    const offenders = listSourceFiles(SRC_DIR).flatMap((file) => {
      const text = readFileSync(file, "utf8")
      const haystack = text.toLowerCase()
      return FORBIDDEN_PILOT_COPY.filter((phrase) =>
        haystack.includes(phrase.toLowerCase())
      ).map((phrase) => `${relative(SRC_DIR, file)}: ${phrase}`)
    })
    expect(offenders).toEqual([])
  })
})
