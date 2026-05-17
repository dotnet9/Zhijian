namespace Zhijian.Services;

public interface IApplicationActionService
{
    void OpenWebsite();

    void OpenRepository();

    void OpenFeedback();

    void OpenFeatureRequest();

    void OpenPullRequests();

    void OpenNewWindow();

    void OpenFileLocation(string filePath);

    Task SetClipboardTextAsync(string text);

    void ShowSuccessMessage(string message);

    void CloseMainWindow();

    void ShowChangelog();

    void ShowThanks();

    void ShowAbout();
}
