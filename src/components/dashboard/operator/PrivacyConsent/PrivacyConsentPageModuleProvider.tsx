import {
  createElement,
  useEffect,
  useState,
  type ReactNode,
} from "react"
import { useSearchParams } from "react-router-dom"

import { privacyConsentPageModuleContext } from "@/components/dashboard/operator/PrivacyConsent/utils/privacyConsentPageModuleContext"
import { createOperatorPrivacyConsentPageModule } from "@/lib/operatorPrivacyConsent/createOperatorPrivacyConsentPageModule"

export function PrivacyConsentPageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const [searchParams] = useSearchParams()
  const initialTabId = searchParams.get("tab")
  const [pageModule] = useState(() =>
    createOperatorPrivacyConsentPageModule({
      initialTabId,
    })
  )

  useEffect(() => {
    pageModule.setActiveTabFromUrl(searchParams.get("tab"))
  }, [pageModule, searchParams])

  return createElement(
    privacyConsentPageModuleContext.Provider,
    { value: pageModule },
    children
  )
}
