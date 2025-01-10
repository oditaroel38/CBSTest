using CBS.Common;

public class SuccessfulTransaction : ITransactionResult
{
    public bool IsSuccessful => true;
    public string Message { get; }

    public SuccessfulTransaction(string message = null)
    {
        Message = message;
    }
}
