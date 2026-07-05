namespace SharedKernel.Domains.Types;

public class CurrentState
{
    public enum CurrentStateType : short
    {
        Deleted = 0,
        Active = 1,
        InActive = 2
    }

}
