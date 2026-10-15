using System.Globalization;
using CodeWF.Avalonia.Lang;

namespace Zhijian.ViewModels;

public sealed class SaveChangesWindowViewModel : ViewModelBase, IDisposable
{
    private readonly string? _documentName;
    public SaveChangesWindowViewModel() : this(null) { }
    public SaveChangesWindowViewModel(string? documentName)
    {
        _documentName = documentName;
        I18nManager.Instance.CultureChanged += OnCultureChanged;
    }
    public string DocumentName => _documentName ?? I18nManager.Instance.GetResource(ZhijianL.Untitled);
    public string Message => string.Format(CultureInfo.CurrentCulture,
        I18nManager.Instance.GetResource(ZhijianL.SaveChangesMessage), DocumentName);
    private void OnCultureChanged(object? sender, EventArgs args)
    {
        OnPropertyChanged(nameof(DocumentName));
        OnPropertyChanged(nameof(Message));
    }
    public void Dispose() => I18nManager.Instance.CultureChanged -= OnCultureChanged;
}
