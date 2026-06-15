import heroFormAccent from "@/assets/svg/hero-form-accent.svg"

import { cn } from "@/lib/utils"

interface AuthFormAccentProps {
  className?: string
}

/**
 * Decorative kitchen/food line-art used on auth and trial form panels.
 * Matches the top-right accent from Figma sign-in shell.
 */
export function AuthFormAccent({ className }: AuthFormAccentProps) {
  return (
    <div
      aria-hidden
      className={cn(
        "pointer-events-none absolute right-0 top-0 z-0 h-[clamp(140px,22vw,210px)] w-[clamp(220px,38vw,367px)] overflow-hidden",
        className
      )}
    >
      <div className="absolute left-[3.67px] top-[-5px]">
        <div className="absolute left-0 top-0 flex h-[209.635px] w-[363.027px] items-center justify-center">
          <div className="-scale-y-100 flex-none rotate-180">
            <div className="relative h-[209.635px] w-[363.027px]">
              <img
                src={heroFormAccent}
                alt=""
                className="absolute inset-0 block size-full max-w-none"
              />
            </div>
          </div>
        </div>
      </div>
      <div className="absolute left-0 top-[-5px] flex h-[210px] w-[367px] items-center justify-center">
        <div className="-scale-y-100 flex-none rotate-180">
          <div
            className="h-[210px] w-[367px]"
            style={{
              backgroundImage:
                "linear-gradient(10.784231689007541deg, rgb(255, 255, 255) 27.237%, rgba(255, 255, 255, 0.2) 71.441%), linear-gradient(87.63101003628996deg, rgb(255, 255, 255) 1.4701%, rgba(255, 255, 255, 0.2) 48.114%)",
            }}
          />
        </div>
      </div>
    </div>
  )
}
