import { X } from "lucide-react"

import { SignupPlanPrice } from "@/components/signup/SignupPlanPrice"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogTitle,
} from "@/components/ui/dialog"
import {
  SIGNUP_COMPARE_ROWS,
  SIGNUP_PLAN_CARD_IDS,
  SIGNUP_PLAN_STEP_COPY,
  type SignupPlanCardId,
} from "@/content/marketing/signupComparePlans"
import type { SignupPlanPriceView } from "@/lib/signupPlanPrices"
import { cn } from "@/lib/utils"

type SignupComparePlansDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  prices: Record<SignupPlanCardId, SignupPlanPriceView | null>
  selected: SignupPlanCardId
  onChoose: (plan: SignupPlanCardId) => void
}

const columnDivider = "border-l border-[#cacaca]"

/** Figma Compare plans `6780:42627`. */
export function SignupComparePlansDialog({
  open,
  onOpenChange,
  prices,
  selected,
  onChoose,
}: SignupComparePlansDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        showCloseButton={false}
        className="flex max-h-[92vh] w-[calc(100vw-2.5rem)] max-w-[1442px] flex-col gap-[45px] overflow-hidden rounded-xl border-0 bg-white p-6 sm:max-w-[1442px] lg:p-[42px]"
      >
        <div className="flex items-start justify-between gap-4">
          <DialogTitle className="m-0 font-jakarta text-[32px] font-medium leading-[normal] text-black lg:text-[46px]">
            Compare plans
          </DialogTitle>
          <DialogClose asChild>
            <Button
              type="button"
              variant="ghost"
              size="icon"
              aria-label="Close"
              className="size-[42px] shrink-0 rounded-[2px] bg-[#dbdbdb] text-[#141414] hover:bg-[#cfcfcf]"
            >
              <X className="size-[18px]" aria-hidden />
            </Button>
          </DialogClose>
        </div>

        <div className="min-h-0 flex-1 overflow-auto rounded-md">
          <table className="w-full min-w-[1000px] table-fixed border-collapse text-left text-[#141414]">
            <colgroup>
              <col className="w-[224px]" />
              {SIGNUP_PLAN_CARD_IDS.map((planId) => (
                <col key={planId} />
              ))}
            </colgroup>
            <thead className="sticky top-0 z-10 bg-[#f0f0f0]">
              <tr>
                <th
                  scope="col"
                  className="w-[224px] p-6 align-bottom font-sans text-lg font-semibold"
                >
                  Feature
                </th>
                {SIGNUP_PLAN_CARD_IDS.map((planId, index) => {
                  const isSelected = planId === selected
                  return (
                    <th
                      key={planId}
                      scope="col"
                      className="px-[11px] py-6 align-top font-normal"
                    >
                      <div
                        className={cn(
                          "flex h-[198px] flex-col justify-between gap-5 pl-[11px]",
                          index > 0 && columnDivider,
                        )}
                      >
                        <div className="flex flex-col gap-[22px]">
                          <p className="m-0 font-jakarta text-xl font-medium">
                            {planId}
                          </p>
                          <SignupPlanPrice
                            price={prices[planId]}
                            amountClassName="text-[28px]"
                          />
                        </div>
                        <Button
                          type="button"
                          variant="outline"
                          onClick={() => {
                            onChoose(planId)
                            onOpenChange(false)
                          }}
                          className={cn(
                            "h-auto w-full rounded-[4px] border-[#4e4e4e] bg-transparent px-[19px] py-[13px] font-sans text-sm font-medium text-[#141414] shadow-none",
                            isSelected &&
                              "border-[#14a74a] bg-[#14a74a] text-white hover:bg-[#14a74a]/90 hover:text-white",
                          )}
                        >
                          {planId === "Pilot"
                            ? SIGNUP_PLAN_STEP_COPY.startPilot
                            : `Choose ${planId}`}
                        </Button>
                      </div>
                    </th>
                  )
                })}
              </tr>
            </thead>
            <tbody className="font-sans text-sm">
              {SIGNUP_COMPARE_ROWS.map((row) => (
                <tr key={row.label}>
                  <th
                    scope="row"
                    className="h-[50px] px-6 align-middle font-semibold"
                  >
                    {row.label}
                  </th>
                  {SIGNUP_PLAN_CARD_IDS.map((planId, index) => (
                    <td key={planId} className="h-[50px] px-[11px] align-middle">
                      <div
                        className={cn(
                          "flex h-[50px] items-center pl-[11px]",
                          index > 0 && columnDivider,
                        )}
                      >
                        {row.values[planId]}
                      </div>
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </DialogContent>
    </Dialog>
  )
}
