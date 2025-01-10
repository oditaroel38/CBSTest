namespace CBS.Models
{
    public class PensionerRemittanceUploadModel
    {
        public required string CONTROLN { get; set; }       
        public string? DEDCODE { get; set; }       
        public decimal? DEDAMOUNT { get; set; }
        public bool IsPosted { get; set; }
    }
}
