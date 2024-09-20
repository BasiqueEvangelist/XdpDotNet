namespace XdgDesktopPortal;

public class UserInteractionAbortedException : Exception
{
    private const string DefaultMessage = "User interaction was aborted";

    public UserInteractionAbortedException() : base(DefaultMessage)
    {
    }

    public UserInteractionAbortedException(string? message) : base(message ?? DefaultMessage)
    {
    }
}