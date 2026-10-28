export type GuestPreferenceChannels = {
  email: boolean
  sms: boolean
}

/** Only the channels that differ from the saved state. */
export type GuestPreferenceChannelChanges = Partial<GuestPreferenceChannels>

/** Keys whose value differs between `initial` and `current`. */
export function diffChannelChanges(
  initial: GuestPreferenceChannels,
  current: GuestPreferenceChannels
): GuestPreferenceChannelChanges {
  const changes: GuestPreferenceChannelChanges = {}
  if (current.email !== initial.email) {
    changes.email = current.email
  }
  if (current.sms !== initial.sms) {
    changes.sms = current.sms
  }
  return changes
}
