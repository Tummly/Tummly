import { createContext, useContext, type ReactNode } from "react"

const WizardLiveValidationContext = createContext<ReadonlySet<string> | null>(
  null
)

function WizardLiveValidationProvider({
  attemptedFields,
  children,
}: {
  attemptedFields: ReadonlySet<string>
  children: ReactNode
}) {
  return (
    <WizardLiveValidationContext.Provider value={attemptedFields}>
      {children}
    </WizardLiveValidationContext.Provider>
  )
}

function useWizardLiveValidation(fieldName: string): boolean {
  const attemptedFields = useContext(WizardLiveValidationContext)
  return attemptedFields?.has(fieldName) ?? false
}

export { WizardLiveValidationProvider, useWizardLiveValidation }
