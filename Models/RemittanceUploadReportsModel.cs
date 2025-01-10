using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CBS.Models
{
    public class RemittanceUploadReportsModel
    {
        public string? CONTROLN { get; set; }
        public string? FULLNAME { get; set; }
        public string? PN { get; set; }
        public double? DEDAMOUNT { get; set; }
    }
}
