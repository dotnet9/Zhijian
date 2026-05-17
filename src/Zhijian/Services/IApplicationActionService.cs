namespace Zhijian.Services;

public interface IApplicationActionService
{
    void OpenWebsite();

    void OpenRepository();

    void OpenNewWindow();

    void OpenFileLocation(string filePath);

    void CloseMainWindow();

    void ShowChangelog();

    void ShowThanks();

    void ShowAbout();
}
