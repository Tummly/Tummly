import {
  clearDemoTourSession,
  isDemoTourActive,
} from "@/lib/productDemo/demoTourSession"
import { PRODUCT_DEMO_BASE } from "@/lib/productDemo/productDemoPaths"

/**
 * Where to send the browser when the session can no longer be used.
 * Public product-demo sessions have no refresh token, so an expired demo
 * restarts the Demo instead of opening operator login.
 */
export function sessionExpiredRedirectPath(): string {
  if (isDemoTourActive()) {
    clearDemoTourSession()
    return PRODUCT_DEMO_BASE
  }
  return "/login"
}
