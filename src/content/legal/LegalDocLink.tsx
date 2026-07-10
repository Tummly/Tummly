import type { ReactNode } from "react"
import { Link } from "react-router-dom"

const legalDocLinkClassName =
  "font-medium text-[#141414] underline underline-offset-2 hover:text-[#141414]"

type LegalDocLinkProps = {
  to: string
  children: ReactNode
}

export function LegalDocLink({ to, children }: LegalDocLinkProps) {
  return (
    <Link to={to} className={legalDocLinkClassName}>
      {children}
    </Link>
  )
}
