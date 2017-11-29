using JushoLatLong.ViewModel;

namespace JushoLatLong.Utils
{
    interface IFileUtil
    {
        string GetSelectedFile(string fileType);
        string GetOutputFolder();
        bool IsFileLocked(string fileName);
        bool ValidateInputFile(MainWindow mainWindow, ActivityViewModel viewModel);
        string PrepareFile(string suffix, MainWindow mainWindow, ActivityViewModel viewModel);
    }
}
