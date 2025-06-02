using ClothesStore.WPF.DAL.Models;

namespace ClothesStore.WPF.Infrastructure;

public interface ICurrentUserService
{ 
    void Set(User? user);
    
    CurrentUser? Get();
}

public class CurrentUserService : ICurrentUserService
{
    private CurrentUser? _currentUser;
    
    public void Set(User? user)
    {
        Interlocked.Exchange(ref _currentUser, user is null ? null : new CurrentUser(user.Id, user.Username, user.PhoneNumber, user.Role));
    }

    public CurrentUser? Get() => _currentUser;
}

public record CurrentUser(Guid Id, string Username, string PhoneNumber, Role Role);