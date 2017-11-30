namespace JushoLatLong.Utils
{
    interface IFileUtil
    {
        string GetSelectedFile(string fileType);
        string GetOutputFolder();
        bool IsFileLocked(string fileName);
        bool IsFileOkToRead(string fileUriString);
        bool IsFileOkToWrite(string fileUriString);
    }
}
