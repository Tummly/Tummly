import { Link, useLocation } from "react-router-dom"

import { ContactPageShell } from "@/components/help-centre/ContactPageShell"
import { Button } from "@/components/ui/button"
import {
  formatHelpCentreQueryReference,
  maskHelpCentreContactEmail,
  type HelpCentreContactSuccessState,
} from "@/lib/helpCentreContactSuccess"

const backHomeClass =
  "h-auto min-h-11 w-fit gap-1.5 rounded-[4px] border border-[#4e4e4e] bg-transparent px-[19px] py-[13px] text-sm font-medium leading-5 text-[#141414] shadow-none hover:border-[#707070] hover:bg-black/5"

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
            <h2 className="m-0 font-jakarta text-[34px] font-medium leading-normal text-black">
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

          <Button asChild variant="outline" className={backHomeClass}>
            <Link to="/">Back to home</Link>
          </Button>
        </>
      }
    />
  )
}
