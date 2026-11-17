namespace MiniCRM.Application;

public interface ICurrentUser
{
    int? UserId { get; }
    bool IsAdministratorOrManager { get; }
}
