namespace XdgDesktopPortal;

public class CancelledByUserException : UserInteractionAbortedException
{
    private const string DefaultMessage = "Operation was cancelled by user";

    public CancelledByUserException() : base(DefaultMessage)
    {
    }
}