import { Link } from "react-router-dom"

import Footer from "@/components/home/Footer"
import { HelpCentreFormPanel } from "@/components/help-centre/HelpCentreFormPanel"
import {
  HELP_CENTRE_CONTACT_URL,
  HELP_CENTRE_MY_QUERIES_URL,
  HELP_CENTRE_URL,
} from "@/config/support"
import { useAuthStore } from "@/stores/authStore"

export default function HelpCentreContactSuccessPage() {
  const token = useAuthStore((state) => state.token)
  const role = useAuthStore((state) => state.role)
  const isOperator = Boolean(token && role === "USER")

  return (
    <>
      <HelpCentreFormPanel>
        <div className="flex flex-col items-center gap-[22px] text-center text-[#232323]">
          <h1 className="m-0 max-w-[478px] text-[36px] font-bold tracking-[-0.72px]">
            Thanks — we&apos;ve received your request
          </h1>
          <p className="m-0 max-w-[318px] text-lg leading-6 tracking-[-0.36px]">
            We&apos;ll review the details and contact you using the details
            provided.
          </p>

          <div className="mt-2 flex flex-col items-center gap-3">
            <Link
              to={HELP_CENTRE_URL}
              className="text-base font-medium text-[#14a74a] underline underline-offset-2"
            >
              Back to Help Centre
            </Link>
            {isOperator && (
              <Link
                to={HELP_CENTRE_MY_QUERIES_URL}
                className="text-base font-medium text-[#14a74a] underline underline-offset-2"
              >
                View my queries
              </Link>
            )}
            <Link
              to={HELP_CENTRE_CONTACT_URL}
              className="text-sm text-[#141414] underline underline-offset-2"
            >
              Submit another request
            </Link>
          </div>
        </div>
      </HelpCentreFormPanel>
      <Footer />
    </>
  )
}
