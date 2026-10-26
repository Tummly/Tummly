import { readFileSync } from "node:fs"
import { dirname, join } from "node:path"
import { fileURLToPath } from "node:url"
import { describe, expect, it } from "vitest"

const homeDir = dirname(fileURLToPath(import.meta.url))

describe("Home upload logo CTA chrome (WS2b Task 9 carry)", () => {
  it("passes an upload handler only when accountWorkspaceAccess allows Manage", () => {
    const home = readFileSync(join(homeDir, "HomePage.tsx"), "utf8")

    expect(home).toMatch(/canWriteOperatorArea\(accountWorkspaceAccess\)/)
  })

  it("hides the upload-logo action when no upload handler is passed", () => {
    const checklist = readFileSync(
      join(homeDir, "HomeSetupChecklist.tsx"),
      "utf8"
    )

    expect(checklist).toMatch(/action\.id\s*!==\s*"upload-logo"\s*\|\|\s*onUploadLogo/)
  })
})
