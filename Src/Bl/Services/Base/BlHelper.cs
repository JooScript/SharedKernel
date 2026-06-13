using static SharedKernel.Core.Enums.Status.Status;

namespace SharedKernel.Bl.Services.Base;

public class BlHelper
{
    public static T NormalizeDbObjToAdd<T>(T dbObject, Guid loggedInUserId) where T : SharedKernelBaseTable
    {
        dbObject.Id = Guid.Empty;
        dbObject.CreatedBy = loggedInUserId;
        dbObject.CreatedAt = DateTime.Now;
        dbObject.CurrentState = enCurrentState.Active;

        return dbObject;
    }

    public static T NormalizeDbObjToUpdate<T>(T dbObject, Guid loggedInUserId) where T : SharedKernelBaseTable
    {
        dbObject.UpdatedBy = loggedInUserId;
        dbObject.UpdatedAt = DateTime.Now;

        return dbObject;
    }
}