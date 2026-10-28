import { renderToStaticMarkup } from "react-dom/server"
import { MemoryRouter } from "react-router-dom"
import { describe, expect, it } from "vitest"

import HelpCentreContactSuccessPage from "@/pages/public/HelpCentreContactSuccessPage"

const SUCCESS_HEADING = "received your enquiry"

describe("HelpCentreContactSuccessPage", () => {
  it("showsConfirmation_withValidSubmissionState", () => {
    const html = renderToStaticMarkup(
      <MemoryRouter
        initialEntries={[
          {
            pathname: "/help-center/contact/success",
            state: { queryId: 42, email: "jane@example.com" },
          },
        ]}
      >
        <HelpCentreContactSuccessPage />
      </MemoryRouter>
    )

    expect(html).toContain(SUCCESS_HEADING)
    expect(html).toContain("TUM-000042")
  })

  it("rendersNoConfirmation_withoutSubmissionState", () => {
    const html = renderToStaticMarkup(
      <MemoryRouter initialEntries={["/help-center/contact/success"]}>
        <HelpCentreContactSuccessPage />
      </MemoryRouter>
    )

    expect(html).not.toContain(SUCCESS_HEADING)
    expect(html).not.toContain("Back to home")
  })
})
