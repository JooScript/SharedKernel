namespace SharedKernel.Da.Contracts;

public interface ISharedKernelViewRepo<Vw> : ISharedKernelQueryRepo<Vw>
    where Vw : class;
