import type { ReactNode } from "react"

type GuestLoopStepHeaderProps = {
  title: string
  description: ReactNode
}

export function GuestLoopStepHeader({
  title,
  description,
}: GuestLoopStepHeaderProps) {
  return (
    <header className="flex flex-col items-center gap-3.5 text-center text-[#232323]">
      <h1 className="m-0 text-[clamp(1.75rem,4vw,2.25rem)] font-bold leading-normal tracking-[-0.72px]">
        {title}
      </h1>
      {typeof description === "string" ? (
        <p className="m-0 max-w-[32rem] text-[clamp(1rem,2.5vw,1.125rem)] leading-6 tracking-[-0.36px]">
          {description}
        </p>
      ) : (
        <div className="m-0 max-w-[32rem] text-[clamp(1rem,2.5vw,1.125rem)] leading-6 tracking-[-0.36px]">
          {description}
        </div>
      )}
    </header>
  )
}
