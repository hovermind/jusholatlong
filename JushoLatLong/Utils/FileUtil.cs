using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;

namespace JushoLatLong.Utils
{
    class FileUtil : IFileUtil
    {
        public string GetSelectedFile(string fileType)
        {
            // create OpenFileDialog
            var fileDialog = new OpenFileDialog
            {
                Title = "Browse File",
                // filter for file extension & default file extension
                DefaultExt = $".{fileType}",
                Filter = $"{fileType.ToUpper()} Files|*.{fileType}",
                Multiselect = false
            };

            // ShowDialog
            bool? result = fileDialog.ShowDialog();

            // return selected file name
            if (result != null && result == true) return fileDialog.FileName;

            // user did not select file
            return "";
        }

        public string GetOutputFolder() {

            var folderDialog = new CommonOpenFileDialog
            {
                Title = "Browse Folder",
                IsFolderPicker = true,
                Multiselect = false
            };

            // ShowDialog
            var result = folderDialog.ShowDialog();

            if (CommonFileDialogResult.Ok.Equals(result)) return folderDialog.FileName;

            // did not select folder
            return "";
        }

        public bool IsFileLocked(string fileName) {

            try {

                var fs = File.Open(fileName, FileMode.Open);
                fs.Close();

                return false;

            } catch (IOException ex) {
                return true;
            }
        }
    }
}
