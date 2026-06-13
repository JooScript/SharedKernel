namespace SharedKernel.Bl.Contracts.Base;

public interface IMapper<Tb, Dto>
{
    Dto ToDto(Tb entity);

    Tb ToEntity(Dto dto);
}