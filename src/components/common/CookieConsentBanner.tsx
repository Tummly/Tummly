import { Link } from "react-router-dom";

import { Button } from "@/components/ui/button";
import { LEGAL_ROUTES } from "@/constants/legalRoutes";
import { cn } from "@/lib/utils";
import { useCookieConsentStore } from "@/stores/cookieConsentStore";

export function CookieConsentBanner() {
  const analytics = useCookieConsentStore((state) => state.analytics);
  const hasHydrated = useCookieConsentStore((state) => state._hasHydrated);
  const acceptAll = useCookieConsentStore((state) => state.acceptAll);
  const rejectNonEssential = useCookieConsentStore(
    (state) => state.rejectNonEssential,
  );

  if (!hasHydrated || analytics !== null) {
    return null;
  }

  return (
    <div
      role="dialog"
      aria-labelledby="cookie-consent-title"
      aria-describedby="cookie-consent-description"
      className={cn(
        "fixed inset-x-0 bottom-0 z-50 border-t border-[#e7e7e7] bg-white/95 p-4 shadow-[0_-8px_30px_rgba(0,0,0,0.08)] backdrop-blur-sm",
        "sm:p-5",
      )}
    >
      <div className="mx-auto flex w-full max-w-7xl flex-col gap-4 lg:flex-row lg:items-center lg:justify-between lg:gap-8">
        <div className="min-w-0 flex-1 space-y-2">
          <p
            id="cookie-consent-title"
            className="m-0 text-base font-semibold text-[#141414]"
          >
            We use cookies
          </p>
          <p
            id="cookie-consent-description"
            className="m-0 text-sm leading-6 text-[#525252]"
          >
            We use essential cookies to keep Tummly working and optional analytics
            cookies to understand how the site is used. You can accept all,
            reject non-essential cookies, or manage your choices in{" "}
            <Link
              to={LEGAL_ROUTES.cookieSettings}
              className="font-medium text-primary underline-offset-4 hover:underline"
            >
              cookie settings
            </Link>
            .
          </p>
        </div>

        <div className="flex w-full flex-col gap-2 sm:w-auto sm:flex-row sm:items-center">
          <Button
            type="button"
            variant="outline"
            size="responsive"
            onClick={rejectNonEssential}
          >
            Reject non-essential
          </Button>
          <Button type="button" size="responsive" onClick={acceptAll}>
            Accept all
          </Button>
        </div>
      </div>
    </div>
  );
}
