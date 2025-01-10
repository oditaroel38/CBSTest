namespace CBS.Models
{
    public class MarinesRemittanceUploadModel
    {
        public required string AFPSN { get; set; }       
        public string? DEDNCODE { get; set; }       
        public decimal? AMOUNT { get; set; }
        public bool IsPosted { get; set; }
    }
}
