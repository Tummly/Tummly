import { Link, useLocation } from "react-router-dom"

import { ContactPageShell } from "@/components/help-centre/ContactPageShell"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import {
  formatHelpCentreQueryReference,
  maskHelpCentreContactEmail,
  type HelpCentreContactSuccessState,
} from "@/lib/helpCentreContactSuccess"

function isSuccessState(value: unknown): value is HelpCentreContactSuccessState {
  if (!value || typeof value !== "object") {
    return false
  }

  const record = value as Record<string, unknown>
  return (
    typeof record.queryId === "number" &&
    Number.isFinite(record.queryId) &&
    typeof record.email === "string" &&
    record.email.trim().length > 0
  )
}

export default function HelpCentreContactSuccessPage() {
  const location = useLocation()
  const state = isSuccessState(location.state) ? location.state : null

  return (
    <ContactPageShell
      panel={
        <>
          <div className="flex flex-col gap-[18px] text-[#141414]">
            <h2 className="m-0 font-jakarta text-[34px] font-medium leading-none text-black">
              We&apos;ve received your enquiry.
            </h2>
            {state ? (
              <>
                <p className="m-0 max-w-[553px] text-lg leading-6">
                  Thank you. We&apos;ll reply to{" "}
                  {maskHelpCentreContactEmail(state.email)}.
                </p>
                <div className="flex flex-wrap items-center gap-3 text-lg leading-6">
                  <span className="font-bold">Reference:</span>
                  <span>
                    {formatHelpCentreQueryReference(state.queryId)}
                  </span>
                </div>
                <p className="m-0 max-w-[553px] text-lg leading-6">
                  Keep this reference for any follow-up.
                </p>
              </>
            ) : (
              <p className="m-0 max-w-[553px] text-lg leading-6">
                Thank you. We&apos;ll review the details and contact you using
                the details provided.
              </p>
            )}
          </div>

          <MarketingButton asChild variant="secondary">
            <Link to="/">Back to home</Link>
          </MarketingButton>
        </>
      }
    />
  )
}
