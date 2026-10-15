using System.Globalization;
using CodeWF.Toolkit.Lang;

namespace Zhijian.ViewModels;

public sealed class SaveChangesWindowViewModel : ViewModelBase, IDisposable
{
    private readonly string? _documentName;
    private bool _disposed;
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
        if (_disposed) return;
        if (!global::Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
        {
            global::Avalonia.Threading.Dispatcher.UIThread.Post(() => OnCultureChanged(sender, args));
            return;
        }
        OnPropertyChanged(nameof(DocumentName));
        OnPropertyChanged(nameof(Message));
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        I18nManager.Instance.CultureChanged -= OnCultureChanged;
    }
}
