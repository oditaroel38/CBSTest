using Data; // Ensure this namespace is included

namespace CBS.Data.Entities
{
    public class CONTROLN_MEMBERSHIP_INFO : IEntity
    {
        public int Id { get; set; }
        public string? SCNO { get; set; }
        public string? CONTROLN { get; set; }
    }
}