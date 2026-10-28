import { Link, Navigate, useLocation } from "react-router-dom"

import { ContactPageShell } from "@/components/help-centre/ContactPageShell"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import { HELP_CENTRE_CONTACT_URL } from "@/config/support"
import {
  formatHelpCentreQueryReference,
  isHelpCentreContactSuccessState,
  maskHelpCentreContactEmail,
} from "@/lib/helpCentreContactSuccess"

export default function HelpCentreContactSuccessPage() {
  const location = useLocation()

  if (!isHelpCentreContactSuccessState(location.state)) {
    return <Navigate to={HELP_CENTRE_CONTACT_URL} replace />
  }

  const state = location.state

  return (
    <ContactPageShell
      panel={
        <>
          <div className="flex flex-col gap-[18px] text-[#141414]">
            <h2 className="m-0 font-jakarta text-[34px] font-medium leading-none text-black">
              We&apos;ve received your enquiry.
            </h2>
            <p className="m-0 max-w-[553px] text-lg leading-6">
              Thank you. We&apos;ll reply to{" "}
              {maskHelpCentreContactEmail(state.email)}.
            </p>
            <div className="flex flex-wrap items-center gap-3 text-lg leading-6">
              <span className="font-bold">Reference:</span>
              <span>{formatHelpCentreQueryReference(state.queryId)}</span>
            </div>
            <p className="m-0 max-w-[553px] text-lg leading-6">
              Keep this reference for any follow-up.
            </p>
          </div>

          <MarketingButton asChild variant="secondary">
            <Link to="/">Back to home</Link>
          </MarketingButton>
        </>
      }
    />
  )
}
