using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CBS.Models
{
    public class FilterReportsModel
    {
        public string? Scno { get; set; }
        public string? Br { get; set; }
        public string? Pn { get; set; }
    }
}
