namespace Zhijian.Services;

public sealed class DisabledApplicationActionService : IApplicationActionService
{
    public void OpenWebsite()
    {
    }

    public void OpenRepository()
    {
    }

    public void OpenFeedback()
    {
    }

    public void OpenFeatureRequest()
    {
    }

    public void OpenPullRequests()
    {
    }

    public void OpenNewWindow()
    {
    }

    public void OpenFileLocation(string filePath)
    {
    }

    public Task SetClipboardTextAsync(string text)
    {
        return Task.CompletedTask;
    }

    public void ShowSuccessMessage(string message)
    {
    }

    public void CloseMainWindow()
    {
    }

    public void ShowChangelog()
    {
    }

    public void ShowThanks()
    {
    }

    public void ShowAbout()
    {
    }
}
