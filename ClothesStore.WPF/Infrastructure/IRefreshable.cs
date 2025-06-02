namespace ClothesStore.WPF.Infrastructure;

public interface IRefreshable
{
    Task RefreshAsync();
}