# Operator leave-dirty is one guard and one confirm Dialog

Settings children that hold a form draft use `src/lib/operatorNavigation/leaveDirtyGuard.ts` for SideNav and browser-back, and the shared confirm Dialog exported as `AccountWorkspaceConfirmDialog`. The Dialog name is historical; **Team & permissions** already reuses it. Do not add a third leave-dirty path.

We rejected copying **Unsaved changes** into each Settings page module. Product lock: Account & workspace tickets 01 and 07.
