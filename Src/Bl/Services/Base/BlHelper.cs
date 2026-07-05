using System.Collections;
using static SharedKernel.Domains.Types.CurrentState;

namespace SharedKernel.Bl.Services.Base;

public class BlHelper
{
    public static T NormalizeDbObjToAdd<T>(T dbObject, Guid loggedInUserId) where T : SharedKernelBaseTable
    {
        dbObject.Id = Guid.Empty;
        dbObject.CreatedBy = loggedInUserId;
        dbObject.CreatedAt = DateTimeOffset.UtcNow;
        dbObject.CurrentState = CurrentStateType.Active;

        return dbObject;
    }

    public static IEnumerable<T> NormalizeDbObjsToAdd<T>(IEnumerable<T> dbObjects, Guid loggedInUserId) where T : SharedKernelBaseTable
        => dbObjects.Select(dbObj => NormalizeDbObjToAdd(dbObj, loggedInUserId));

    public static T NormalizeDbObjToUpdate<T>(T dbObject, Guid loggedInUserId) where T : SharedKernelBaseTable
    {
        dbObject.UpdatedBy = loggedInUserId;
        dbObject.UpdatedAt = DateTimeOffset.UtcNow;

        return dbObject;
    }
}