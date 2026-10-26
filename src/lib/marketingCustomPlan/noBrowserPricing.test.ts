import { readdirSync, readFileSync, statSync } from "node:fs"
import { join, relative, resolve } from "node:path"
import { describe, expect, it } from "vitest"

const SRC_ROOT = resolve(__dirname, "../..")

function listSourceFiles(dir: string): string[] {
  const files: string[] = []
  for (const name of readdirSync(dir)) {
    const path = join(dir, name)
    if (statSync(path).isDirectory()) {
      files.push(...listSourceFiles(path))
    } else if (/\.(ts|tsx)$/.test(name) && !name.endsWith(".test.ts")) {
      files.push(path)
    }
  }
  return files
}

const SOURCES = listSourceFiles(SRC_ROOT).map((path) => ({
  path: relative(SRC_ROOT, path),
  text: readFileSync(path, "utf8"),
}))

function filesMatching(pattern: RegExp): string[] {
  return SOURCES.filter((file) => pattern.test(file.text)).map((file) => file.path)
}

/** Custom Plan calculator and signup surfaces that Task 8 owns. */
const TASK8_SCOPE = /^(components\/(marketing\/custom-plan|signup)|lib\/(marketingCustomPlan|signupPlanIntent)|pages\/(auth\/SignupOnboardingPage|public\/CustomPlanPage)|schemas\/signupOnboarding)/

describe("no browser price maths in src", () => {
  it("has no browser estimate, pack price or annual fallback code", () => {
    expect(
      filesMatching(/estimateCustomPlanV2|packMonthlyNetPence|ANNUAL_FALLBACK_FACTOR|\* 0\.85/),
    ).toEqual([])
  })

  it("has no static VAT note in the Custom Plan or signup code", () => {
    // Manage Plan and Shop VAT copy sit outside WS3a scope (see Task 7 review).
    const inScope = SOURCES.filter(
      (file) =>
        TASK8_SCOPE.test(file.path) &&
        /VAT is added at checkout when applicable/.test(file.text),
    ).map((file) => file.path)
    expect(inScope).toEqual([])
  })

  it("has no self-serve limit or 30-Location copy in the Custom Plan or signup code", () => {
    // Pricing FAQ copy (pricingPage.ts) is replaced by Task 9.
    const inScope = SOURCES.filter(
      (file) =>
        TASK8_SCOPE.test(file.path) && /self-serve limit|30-Location/.test(file.text),
    ).map((file) => file.path)
    expect(inScope).toEqual([])
  })
})
