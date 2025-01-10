namespace CBS.Models.Shared
{
    public class Result
    {
        public Result()
        {

        }
        public bool IsSuccessful { get; set; }

        public string Message { get; set; }

        public object Response { get; set; }
    }
}
