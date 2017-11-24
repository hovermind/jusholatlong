namespace JushoLatLong.Utils
{
    interface IFileUtil
    {
        string GetSelectedFile(string fileType);
        string GetOutputFolder();
        bool IsFileLocked(string fileName);
    }
}
