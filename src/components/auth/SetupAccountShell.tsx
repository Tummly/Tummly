import type { ReactNode } from "react"

interface SetupAccountShellProps {
  children: ReactNode
}

function SetupAccountShell({ children }: SetupAccountShellProps) {
  return (
    <div className="flex min-h-dvh flex-col bg-[#FAFAFA] text-[#111827]">
      {children}
    </div>
  )
}

interface SetupAccountStatusProps {
  title: string
  message?: string
  tone?: "loading" | "error"
}

function SetupAccountStatus({
  title,
  message,
  tone = "loading",
}: SetupAccountStatusProps) {
  return (
    <SetupAccountShell>
      <main className="flex flex-1 items-center justify-center px-6 py-16">
        <div
          className="max-w-[520px] text-center"
          role={tone === "loading" ? "status" : undefined}
          aria-live={tone === "loading" ? "polite" : undefined}
          aria-label={tone === "loading" ? title : undefined}
        >
          {tone === "loading" ? (
            <div className="mb-6 flex justify-center">
              <div className="h-8 w-8 animate-spin rounded-full border-2 border-[#d2d2d2] border-t-[#14a74a]" />
            </div>
          ) : null}
          <h1
            className={`text-[34px] font-bold ${
              tone === "error" ? "text-red-500" : "text-[#111827]"
            } ${message ? "mb-4" : ""}`}
          >
            {title}
          </h1>
          {message ? (
            <p className="text-[16px] leading-relaxed text-[#6B7280]">
              {message}
            </p>
          ) : null}
        </div>
      </main>
    </SetupAccountShell>
  )
}

export { SetupAccountShell, SetupAccountStatus }
