using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CBS.Models
{
    public class GetComputedArrearsDataModel
    {
        public string? Scno { get; set; }
        public string? Br { get; set; }
        public decimal? Interest { get; set; }
    }
}
