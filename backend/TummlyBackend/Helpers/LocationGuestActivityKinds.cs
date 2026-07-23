namespace TummlyBackend.Helpers
{
    public static class LocationGuestActivityKinds
    {
        public const string GuestJoined = "guest-joined";
        public const string Feedback = "feedback";
        public const string NoteAdded = "note-added";
        public const string TagApplied = "tag-applied";
        public const string TagRemoved = "tag-removed";
        public const string ProfileEdited = "profile-edited";
        public const string ClassificationSucceeded = "classification-succeeded";
        public const string ClassificationFailed = "classification-failed";

        /// <summary>
        /// Activity-type filter tokens (toolbar) → persisted kinds (OR within type).
        /// </summary>
        public static IReadOnlyList<string>? KindsForFilterType(string type)
        {
            return type.Trim().ToLowerInvariant() switch
            {
                "guest-joined" => [GuestJoined],
                "feedback" => [Feedback],
                "classification" =>
                [
                    ClassificationSucceeded,
                    ClassificationFailed,
                ],
                "note" => [NoteAdded],
                "tag" => [TagApplied, TagRemoved],
                "profile-update" or "profile-edited" => [ProfileEdited],
                _ => null,
            };
        }
    }
}
