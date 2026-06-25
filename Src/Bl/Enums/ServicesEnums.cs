namespace SharedKernel.Bl.Enums;

public class ServicesEnums
{
    public enum DeleteType : short
    {
        SoftDelete = 1,
        HardDelete = 2
    }

    public enum SaveModeType : short
    {
        Insert = 1,
        Update = 2,
        Readonly = 3
    }
}
