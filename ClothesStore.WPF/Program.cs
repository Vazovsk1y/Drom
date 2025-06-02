namespace ClothesStore.WPF;

public class Program
{
    private const string MutexId = "913a70c5-3a1e-5597-81bd-b26e5d8df3f8";
    private static Mutex? _mutex;
    
    [STAThread]
    public static void Main(string[] args)
    {
        _mutex = new Mutex(false, MutexId, out var createdNew);
        if (!createdNew)
        {
            return;
        }
        
        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}