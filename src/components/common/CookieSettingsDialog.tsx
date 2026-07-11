import { useEffect, useId, useRef, useState, type ReactNode } from "react";
import { useNavigate } from "react-router-dom";

import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import { LEGAL_ROUTES } from "@/constants/legalRoutes";
import { cn } from "@/lib/utils";
import { useCookieConsentStore } from "@/stores/cookieConsentStore";
import { useCookieSettingsUiStore } from "@/stores/cookieSettingsUiStore";

const SAVE_CLOSE_DELAY_MS = 1600;

export function CookieSettingsDialog() {
  const navigate = useNavigate();
  const analyticsId = useId();
  const closeTimerRef = useRef<number | null>(null);

  const isOpen = useCookieSettingsUiStore((state) => state.isOpen);
  const setOpen = useCookieSettingsUiStore((state) => state.setOpen);
  const close = useCookieSettingsUiStore((state) => state.close);

  const storedAnalytics = useCookieConsentStore((state) => state.analytics);
  const setAnalytics = useCookieConsentStore((state) => state.setAnalytics);

  const [analyticsEnabled, setAnalyticsEnabled] = useState(false);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    setAnalyticsEnabled(storedAnalytics ?? false);
    setSaved(false);
  }, [isOpen, storedAnalytics]);

  useEffect(() => {
    return () => {
      if (closeTimerRef.current != null) {
        window.clearTimeout(closeTimerRef.current);
      }
    };
  }, []);

  function handleOpenChange(open: boolean) {
    if (closeTimerRef.current != null) {
      window.clearTimeout(closeTimerRef.current);
      closeTimerRef.current = null;
    }

    setOpen(open);
    if (!open) {
      setSaved(false);
    }
  }

  function handleSave() {
    setAnalytics(analyticsEnabled);
    setSaved(true);

    if (closeTimerRef.current != null) {
      window.clearTimeout(closeTimerRef.current);
    }

    closeTimerRef.current = window.setTimeout(() => {
      closeTimerRef.current = null;
      close();
      setSaved(false);
    }, SAVE_CLOSE_DELAY_MS);
  }

  function handleCookiePolicyClick() {
    if (closeTimerRef.current != null) {
      window.clearTimeout(closeTimerRef.current);
      closeTimerRef.current = null;
    }

    close();
    navigate(LEGAL_ROUTES.cookiePolicy);
  }

  return (
    <Dialog open={isOpen} onOpenChange={handleOpenChange}>
      <DialogContent className="gap-7 bg-white sm:max-w-[480px]">
        <DialogHeader className="gap-2">
          <DialogTitle className="text-[clamp(1.5rem,3vw,1.875rem)] tracking-[-0.4px]">
            Cookie settings
          </DialogTitle>
          <DialogDescription className="text-sm leading-6 text-[#525252]">
            Choose whether optional analytics cookies may run in this browser.
            Essential cookies stay on so sign-in, security, and your preference
            stay reliable.
          </DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-4">
          <p className="m-0 rounded-lg bg-[#f7f7f7] px-4 py-3 text-sm leading-6 text-[#525252]">
            <span className="font-medium text-[#141414]">Essential cookies</span>
            {" — "}
            always on for authentication, session security, and remembering this
            choice.
          </p>

          <div className="rounded-xl border border-[#e7e7e7] bg-white p-4 shadow-[0_1px_0_rgba(20,20,20,0.03)]">
            <div className="flex items-start gap-3">
              <Checkbox
                id={analyticsId}
                checked={analyticsEnabled}
                onCheckedChange={(checked) =>
                  setAnalyticsEnabled(checked === true)
                }
                className="mt-0.5"
              />
              <div className="min-w-0 space-y-1">
                <Label
                  htmlFor={analyticsId}
                  className="text-base font-semibold text-[#141414]"
                >
                  Analytics cookies
                </Label>
                <p className="m-0 text-sm leading-6 text-[#525252]">
                  Help us understand how visitors use Tummly through Google
                  Analytics. No advertising cookies are used.
                </p>
              </div>
            </div>
          </div>
        </div>

        <DialogFooter className="gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:gap-3">
            <Button
              type="button"
              size="responsive"
              onClick={handleSave}
              disabled={saved}
            >
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

          <button
            type="button"
            onClick={handleCookiePolicyClick}
            className="shrink-0 self-start whitespace-nowrap text-sm font-medium text-primary underline-offset-4 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/30 sm:self-center"
          >
            Cookie Policy
          </button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

export function CookieSettingsTrigger({
  children = "Cookie settings",
  className,
}: {
  children?: ReactNode;
  className?: string;
}) {
  const open = useCookieSettingsUiStore((state) => state.open);

  return (
    <button type="button" onClick={open} className={className}>
      {children}
    </button>
  );
}
