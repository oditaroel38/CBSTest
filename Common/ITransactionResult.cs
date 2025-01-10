namespace CBS.Common
{
    public interface ITransactionResult
    {
        bool IsSuccessful { get; }
        string Message { get; }
    }
}
