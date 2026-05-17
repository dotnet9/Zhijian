namespace Zhijian.ViewModels;

public sealed class SaveChangesWindowViewModel : ViewModelBase
{
    public SaveChangesWindowViewModel()
        : this("未命名")
    {
    }

    public SaveChangesWindowViewModel(string documentName)
    {
        DocumentName = documentName;
    }

    public string DocumentName { get; }

    public string Message => $"是否保存对“{DocumentName}”的更改？";
}
