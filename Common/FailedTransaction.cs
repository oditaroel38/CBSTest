using CBS.Common;

public class FailedTransaction : ITransactionResult
{
    public bool IsSuccessful => false;
    public string Message { get; }

    public FailedTransaction(string message)
    {
        Message = message;
    }
}
