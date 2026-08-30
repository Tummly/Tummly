using System.ComponentModel.DataAnnotations;

namespace TummlyBackend.Models
{
    /// <summary>
    /// Platform document counter per prefix + UK calendar year (ticket 17).
    /// </summary>
    public class TummlyDocumentSequence
    {
        public const string PrefixTm = "TM";

        public const string PrefixTcn = "TCN";

        [MaxLength(8)]
        public string DocumentPrefix { get; set; } = string.Empty;

        public int Year { get; set; }

        /// <summary>Next number to allocate (starts at 1).</summary>
        public int NextNumber { get; set; } = 1;
    }
}
