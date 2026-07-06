import { Link } from "react-router-dom"

import Footer from "@/components/home/Footer"
import { HelpCentreFormPanel } from "@/components/help-centre/HelpCentreFormPanel"
import {
  HELP_CENTRE_CONTACT_URL,
  HELP_CENTRE_MY_QUERIES_URL,
  HELP_CENTRE_URL,
} from "@/config/support"
import { cn } from "@/lib/utils"
import { useAuthStore } from "@/stores/authStore"

const successLinkClass =
  "text-sm font-medium underline underline-offset-2 sm:text-base"

export default function HelpCentreContactSuccessPage() {
  const token = useAuthStore((state) => state.token)
  const role = useAuthStore((state) => state.role)
  const isOperator = Boolean(token && role === "USER")

  return (
    <div className="flex w-full flex-1 flex-col bg-white">
      <HelpCentreFormPanel className="flex flex-1 flex-col justify-center">
        <div className="flex w-full flex-col items-center text-center text-[#232323]">
          <div className="flex flex-col items-center gap-[22px]">
            <h1 className="m-0 max-w-[478px] text-[36px] font-bold tracking-[-0.72px]">
              Thanks — we&apos;ve received your request
            </h1>
            <p className="m-0 max-w-[318px] text-lg leading-6 tracking-[-0.36px]">
              We&apos;ll review the details and contact you using the details
              provided.
            </p>
          </div>

          <nav
            aria-label="Help Centre next steps"
            className="mt-[50px] flex w-full items-center justify-between gap-4"
          >
            <Link
              to={HELP_CENTRE_URL}
              className={cn(successLinkClass, "text-[#14a74a]")}
            >
              Back to Help Centre
            </Link>
            {isOperator && (
              <Link
                to={HELP_CENTRE_MY_QUERIES_URL}
                className={cn(successLinkClass, "text-[#14a74a]")}
              >
                View my queries
              </Link>
            )}
            <Link
              to={HELP_CENTRE_CONTACT_URL}
              className={cn(successLinkClass, "text-[#141414]")}
            >
              Submit another request
            </Link>
          </nav>
        </div>
      </HelpCentreFormPanel>
      <div className="mt-auto shrink-0">
        <Footer />
      </div>
    </div>
  )
}
