const RECAPTCHA_SCRIPT_ID = "tummly-recaptcha-v3"
const RECAPTCHA_ACTION = "guest_feedback"

type GrecaptchaApi = {
  ready: (callback: () => void) => void
  execute: (siteKey: string, options: { action: string }) => Promise<string>
}

declare global {
  interface Window {
    grecaptcha?: GrecaptchaApi
  }
}

export function getRecaptchaSiteKey(): string | null {
  const key = import.meta.env.VITE_RECAPTCHA_SITE_KEY
  if (typeof key !== "string") {
    return null
  }
  const trimmed = key.trim()
  return trimmed.length > 0 ? trimmed : null
}

function loadRecaptchaScript(siteKey: string): Promise<void> {
  if (typeof document === "undefined") {
    return Promise.reject(new Error("reCAPTCHA unavailable."))
  }

  const existing = document.getElementById(RECAPTCHA_SCRIPT_ID)
  if (existing != null) {
    return waitForGrecaptcha()
  }

  return new Promise((resolve, reject) => {
    const script = document.createElement("script")
    script.id = RECAPTCHA_SCRIPT_ID
    script.src = `https://www.google.com/recaptcha/api.js?render=${encodeURIComponent(siteKey)}`
    script.async = true
    script.onload = () => {
      void waitForGrecaptcha().then(resolve).catch(reject)
    }
    script.onerror = () => {
      reject(new Error("Unable to load security check."))
    }
    document.head.appendChild(script)
  })
}

function waitForGrecaptcha(): Promise<void> {
  return new Promise((resolve, reject) => {
    const started = Date.now()
    const tick = () => {
      if (window.grecaptcha?.ready != null) {
        window.grecaptcha.ready(() => {
          resolve()
        })
        return
      }
      if (Date.now() - started > 10_000) {
        reject(new Error("Security check timed out."))
        return
      }
      window.setTimeout(tick, 50)
    }
    tick()
  })
}

/**
 * Runs reCAPTCHA v3 for Guest Form submit. Returns null when site key is unset
 * (local / preview without captcha). Throws when the site key is set but execute fails.
 */
export async function executeGuestFeedbackRecaptcha(): Promise<string | null> {
  const siteKey = getRecaptchaSiteKey()
  if (siteKey == null) {
    return null
  }

  await loadRecaptchaScript(siteKey)
  const api = window.grecaptcha
  if (api?.execute == null) {
    throw new Error("Security check failed. Please try again.")
  }

  const token = await api.execute(siteKey, { action: RECAPTCHA_ACTION })
  const trimmed = token?.trim() ?? ""
  if (trimmed.length === 0) {
    throw new Error("Security check failed. Please try again.")
  }

  return trimmed
}

export const GUEST_FEEDBACK_RECAPTCHA_ACTION = RECAPTCHA_ACTION
