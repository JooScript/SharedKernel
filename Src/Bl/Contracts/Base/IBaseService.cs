namespace SharedKernel.Bl.Contracts.Base;

/// <summary>
/// Provides a base contract for service operations with standard CRUD functionality
/// </summary>
/// <typeparam name="Tb">The entity type (typically representing a database entity)</typeparam>
/// <typeparam name="Dto">The Data Transfer Object type for input/output operations</typeparam>
public interface IBaseService<Tb, Dto>
    : IMapper<Tb, Dto>,
    ICmdBaseService<Dto>,
    IQryBaseService<Dto>,
    IValidationBaseService<Tb, Dto>
    where Tb : SharedKernelBaseTable
    where Dto : class
{

}