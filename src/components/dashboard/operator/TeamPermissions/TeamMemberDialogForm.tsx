import { CheckboxLabel } from "@/components/ui/checkbox-label"
import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Textarea } from "@/components/ui/textarea"
import {
  ACCOUNT_WORKSPACE_FIELD_HELPER_CLASS,
  ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS,
  ACCOUNT_WORKSPACE_SELECT_ITEM_CLASS,
  ACCOUNT_WORKSPACE_SELECT_MENU_CLASS,
  ACCOUNT_WORKSPACE_SELECT_TRIGGER_CLASS,
  ACCOUNT_WORKSPACE_TEXT_INPUT_CLASS,
} from "@/lib/operatorAccountWorkspace/accountWorkspacePresentation"
import { TEAM_PERMISSIONS_PAGE_COPY as copy } from "@/lib/operatorTeamPermissions/teamPermissionsPresentation"
import {
  TEAM_PERMISSIONS_INVITE_DIVIDER_CLASS,
  TEAM_PERMISSIONS_INVITE_FIELD_STACK_CLASS,
  TEAM_PERMISSIONS_INVITE_FORM_ROW_CLASS,
  TEAM_PERMISSIONS_INVITE_FORM_STACK_CLASS,
  TEAM_PERMISSIONS_INVITE_MESSAGE_FIELD_CLASS,
  TEAM_PERMISSIONS_INVITE_MESSAGE_SECTION_CLASS,
  TEAM_PERMISSIONS_INVITE_TEXTAREA_CLASS,
} from "@/lib/operatorTeamPermissions/teamPermissionsPresentation"

export type TeamMemberFormValues = {
  email: string
  fullName: string
  permissionRole: string
  locationScope: "all" | "named"
  namedLocationIds: number[]
}

type TeamMemberDialogFormProps = {
  idPrefix: string
  values: TeamMemberFormValues
  onChange: (values: TeamMemberFormValues) => void
  roleOptions: readonly string[]
  locations: ReadonlyArray<{ id: number; name: string }>
  isSingleLocation: boolean
  busy?: boolean
  readOnly?: boolean
  readOnlyIdentity?: boolean
  showMessage?: boolean
  message?: string
  onMessageChange?: (message: string) => void
  emailError?: string | null
}

export function TeamMemberDialogForm({
  idPrefix,
  values,
  onChange,
  roleOptions,
  locations,
  isSingleLocation,
  busy = false,
  readOnly = false,
  readOnlyIdentity = false,
  showMessage = false,
  message = "",
  onMessageChange,
  emailError = null,
}: TeamMemberDialogFormProps) {
  const disabled = busy || readOnly
  const identityLocked = disabled || readOnlyIdentity

  return (
    <div className={TEAM_PERMISSIONS_INVITE_FORM_STACK_CLASS}>
      <div className={TEAM_PERMISSIONS_INVITE_FORM_ROW_CLASS}>
        <div className={TEAM_PERMISSIONS_INVITE_FIELD_STACK_CLASS}>
          <label
            htmlFor={`${idPrefix}-email`}
            className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
          >
            {copy.email}
          </label>
          <Input
            id={`${idPrefix}-email`}
            value={values.email}
            disabled={identityLocked}
            readOnly={identityLocked}
            placeholder={copy.emailPlaceholder}
            className={ACCOUNT_WORKSPACE_TEXT_INPUT_CLASS}
            onChange={(event) =>
              onChange({ ...values, email: event.target.value })
            }
          />
          {emailError != null ? (
            <p className="text-sm text-destructive" role="alert">
              {emailError}
            </p>
          ) : null}
        </div>

        <div className={TEAM_PERMISSIONS_INVITE_FIELD_STACK_CLASS}>
          <label
            htmlFor={`${idPrefix}-full-name`}
            className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
          >
            {copy.fullName}
          </label>
          <Input
            id={`${idPrefix}-full-name`}
            value={values.fullName}
            disabled={identityLocked}
            readOnly={identityLocked}
            placeholder={copy.fullNamePlaceholder}
            className={ACCOUNT_WORKSPACE_TEXT_INPUT_CLASS}
            onChange={(event) =>
              onChange({ ...values, fullName: event.target.value })
            }
          />
        </div>
      </div>

      <hr aria-hidden className={TEAM_PERMISSIONS_INVITE_DIVIDER_CLASS} />

      <div className={TEAM_PERMISSIONS_INVITE_FORM_ROW_CLASS}>
        <div className={TEAM_PERMISSIONS_INVITE_FIELD_STACK_CLASS}>
          <label
            htmlFor={`${idPrefix}-role`}
            className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
          >
            {copy.role}
          </label>
          <Select
            value={values.permissionRole}
            onValueChange={(value) =>
              onChange({ ...values, permissionRole: value })
            }
            disabled={disabled}
          >
            <SelectTrigger
              id={`${idPrefix}-role`}
              className={ACCOUNT_WORKSPACE_SELECT_TRIGGER_CLASS}
            >
              <SelectValue placeholder={copy.rolePlaceholder} />
            </SelectTrigger>
            <SelectContent
              position="popper"
              align="start"
              className={ACCOUNT_WORKSPACE_SELECT_MENU_CLASS}
            >
              {roleOptions.map((role) => (
                <SelectItem
                  key={role}
                  value={role}
                  className={ACCOUNT_WORKSPACE_SELECT_ITEM_CLASS}
                >
                  {role}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {isSingleLocation ? null : (
          <div className={TEAM_PERMISSIONS_INVITE_FIELD_STACK_CLASS}>
            <label
              htmlFor={`${idPrefix}-location-access`}
              className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
            >
              {copy.locationAccess}
            </label>
            <Select
              value={values.locationScope}
              onValueChange={(value) =>
                onChange({
                  ...values,
                  locationScope: value as "all" | "named",
                })
              }
              disabled={disabled}
            >
              <SelectTrigger
                id={`${idPrefix}-location-access`}
                className={ACCOUNT_WORKSPACE_SELECT_TRIGGER_CLASS}
              >
                <SelectValue placeholder={copy.locationAccessPlaceholder} />
              </SelectTrigger>
              <SelectContent
                position="popper"
                align="start"
                className={ACCOUNT_WORKSPACE_SELECT_MENU_CLASS}
              >
                <SelectItem
                  value="all"
                  className={ACCOUNT_WORKSPACE_SELECT_ITEM_CLASS}
                >
                  {copy.allLocations}
                </SelectItem>
                <SelectItem
                  value="named"
                  className={ACCOUNT_WORKSPACE_SELECT_ITEM_CLASS}
                >
                  {copy.selectedLocations}
                </SelectItem>
              </SelectContent>
            </Select>
            {values.locationScope === "all" ? (
              <p className={ACCOUNT_WORKSPACE_FIELD_HELPER_CLASS}>
                {copy.allLocationsHelper}
              </p>
            ) : readOnly ? (
              <p className={ACCOUNT_WORKSPACE_FIELD_HELPER_CLASS}>
                {values.namedLocationIds
                  .map(
                    (id) =>
                      locations.find((location) => location.id === id)?.name
                      ?? `#${id}`
                  )
                  .join(", ")}
              </p>
            ) : (
              locations.map((location) => (
                <CheckboxLabel
                  key={location.id}
                  id={`${idPrefix}-loc-${location.id}`}
                  checked={values.namedLocationIds.includes(location.id)}
                  disabled={disabled}
                  onCheckedChange={(checked) => {
                    onChange({
                      ...values,
                      namedLocationIds: checked
                        ? [...values.namedLocationIds, location.id]
                        : values.namedLocationIds.filter(
                            (id) => id !== location.id
                          ),
                    })
                  }}
                >
                  {location.name}
                </CheckboxLabel>
              ))
            )}
          </div>
        )}
      </div>

      {showMessage ? (
        <>
          <hr aria-hidden className={TEAM_PERMISSIONS_INVITE_DIVIDER_CLASS} />

          <div className={TEAM_PERMISSIONS_INVITE_MESSAGE_SECTION_CLASS}>
            <div className={TEAM_PERMISSIONS_INVITE_MESSAGE_FIELD_CLASS}>
              <label
                htmlFor={`${idPrefix}-message`}
                className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
              >
                {copy.message}
              </label>
              <Textarea
                id={`${idPrefix}-message`}
                placeholder={copy.messagePlaceholder}
                value={message}
                disabled={disabled}
                className={TEAM_PERMISSIONS_INVITE_TEXTAREA_CLASS}
                onChange={(event) => onMessageChange?.(event.target.value)}
              />
            </div>
          </div>
        </>
      ) : null}
    </div>
  )
}
