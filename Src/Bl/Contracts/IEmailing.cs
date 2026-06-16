namespace SharedKernel.Bl.Contracts;

public interface IEmailing
{
    Task SendAsync(string email, string subject, string message);
}
