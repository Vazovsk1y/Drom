using Drom.WPF.DAL.Models;

namespace Drom.WPF.Infrastructure;

public interface ICurrentUserService
{ 
    void Set(User? user);
    
    CurrentUser? Get();
}

public class CurrentUserService : ICurrentUserService
{
    private CurrentUser? _currentUser;
    private readonly Lock _setLock = new();
    
    public void Set(User? user)
    {
        using var scope = _setLock.EnterScope();
        _currentUser = user is null ? null : new CurrentUser(user.Id, user.Username, user.PhoneNumber);
    }

    public CurrentUser? Get() => _currentUser;
}

public record CurrentUser(Guid Id, string Username, string PhoneNumber);