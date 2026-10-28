import { Outlet } from "react-router-dom"

import { StaffDashboardHeader } from "@/components/dashboard/staff/StaffDashboardHeader"
import { StaffEnvironmentBanner } from "@/components/dashboard/staff/StaffEnvironmentBanner"

export default function StaffDashboardLayout() {
  return (
    <div className="flex min-h-dvh flex-col bg-background">
      <StaffEnvironmentBanner />
      <StaffDashboardHeader />
      <main className="flex min-h-0 flex-1 flex-col">
        <Outlet />
      </main>
    </div>
  )
}
