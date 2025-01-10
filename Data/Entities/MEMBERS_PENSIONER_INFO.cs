using Data; // Ensure this namespace is included

namespace CBS.Data.Entities
{
    public class MEMBERS_PENSIONER_INFO : IEntity
    {
        public string? SCNO { get; set; }
        public string? PENID { get; set; }
        public DateTime? PENIDXDT { get; set; }
        public bool? ISPEN { get; set; }
        public string? CONTROLN { get; set; }
        public string? CONTROLN2 { get; set; }
        public DateTime? PENDATE { get; set; }
        public string? BR { get; set; }
    }
}