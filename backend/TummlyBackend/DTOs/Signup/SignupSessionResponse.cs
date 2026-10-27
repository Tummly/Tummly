using System.Text.Json.Serialization;

namespace TummlyBackend.DTOs.Signup
{
    public class SignupSessionResponse
    {
        /// <summary>Null for a Complete row: the key is left out of the body.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Guid? SessionToken { get; set; }

        public string Email { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }
}
