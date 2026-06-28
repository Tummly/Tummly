import { useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";
import { useCookieConsentStore } from "@/stores/cookieConsentStore";

export function CookiePreferences() {
  const storedAnalytics = useCookieConsentStore((state) => state.analytics);
  const setAnalytics = useCookieConsentStore((state) => state.setAnalytics);
  const [analyticsEnabled, setAnalyticsEnabled] = useState(false);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    setAnalyticsEnabled(storedAnalytics ?? false);
  }, [storedAnalytics]);

  function handleSave() {
    setAnalytics(analyticsEnabled);
    setSaved(true);
    window.setTimeout(() => setSaved(false), 2500);
  }

  return (
    <section
      aria-labelledby="cookie-preferences-title"
      className="mx-auto w-full max-w-7xl px-4 py-10 sm:px-6 md:px-10 lg:px-16 xl:px-20 2xl:max-w-[108rem] 2xl:px-45"
    >
      <div className="rounded-2xl border border-[#e7e7e7] bg-[#fafafa] p-5 sm:p-6">
        <div className="flex flex-col gap-5">
          <div className="space-y-2">
            <h2
              id="cookie-preferences-title"
              className="m-0 text-[clamp(1.375rem,2.5vw,1.625rem)] font-bold leading-normal text-[#141414]"
            >
              Your cookie preferences
            </h2>
            <p className="m-0 text-sm leading-6 text-[#525252]">
              Essential cookies are always on because they are required for sign
              in, security and saving these preferences. Analytics cookies are
              optional.
            </p>
          </div>

          <div className="flex flex-col gap-4">
            <div className="flex items-start gap-3 rounded-xl border border-[#e7e7e7] bg-white p-4">
              <Checkbox id="essential-cookies" checked disabled />
              <div className="space-y-1">
                <Label
                  htmlFor="essential-cookies"
                  className="text-base font-medium text-[#141414]"
                >
                  Essential cookies
                </Label>
                <p className="m-0 text-sm leading-6 text-[#525252]">
                  Required for authentication, session management and remembering
                  your cookie choices.
                </p>
              </div>
            </div>

            <div className="flex items-start gap-3 rounded-xl border border-[#e7e7e7] bg-white p-4">
              <Checkbox
                id="analytics-cookies"
                checked={analyticsEnabled}
                onCheckedChange={(checked) =>
                  setAnalyticsEnabled(checked === true)
                }
              />
              <div className="space-y-1">
                <Label
                  htmlFor="analytics-cookies"
                  className="text-base font-medium text-[#141414]"
                >
                  Analytics cookies
                </Label>
                <p className="m-0 text-sm leading-6 text-[#525252]">
                  Help us understand how visitors use the site through Google
                  Analytics. No advertising cookies are used.
                </p>
              </div>
            </div>
          </div>

          <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
            <Button type="button" size="responsive" onClick={handleSave}>
              Save preferences
            </Button>
            <p
              aria-live="polite"
              className={cn(
                "m-0 text-sm text-[#525252] transition-opacity",
                saved ? "opacity-100" : "opacity-0",
              )}
            >
              Preferences saved.
            </p>
          </div>
        </div>
      </div>
    </section>
  );
}
