namespace CBS.Models
{
    public class ArmyRemittanceUploadModel
    {
        public required string SERIALNR { get; set; }       
        public string? DEDCODE { get; set; }       
        public decimal? AMOUNT { get; set; }
        public bool IsPosted { get; set; }
    }
}
