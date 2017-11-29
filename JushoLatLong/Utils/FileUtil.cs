using JushoLatLong.ViewModel;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
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

        public string GetOutputFolder()
        {

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

        public bool IsFileLocked(string fileName)
        {

            try
            {

                var fs = File.Open(fileName, FileMode.Open);
                fs.Close();

                return false;

            }
            catch (IOException ex)
            {

                return ex != null;
            }
        }

        public bool ValidateInputFile(MainWindow mainWindow, ActivityViewModel viewModel)
        {
            // check input csv file exists & not locked
            if (!File.Exists(viewModel.SelectedFile) || IsFileLocked(viewModel.SelectedFile))
            {
                if (!File.Exists(viewModel.SelectedFile)) mainWindow.ShowMessage("File does not exist, please select again!");
                else mainWindow.ShowMessage("File is locked, please close it & try again!");

                return false;
            }

            return true;
        }

        public string PrepareFile(string suffix, MainWindow mainWindow, ActivityViewModel viewModel)
        {
            // ouput folder
            if (String.IsNullOrEmpty(viewModel.OutputFolder)) mainWindow.SetDefaultOutputFolder(viewModel.SelectedFile);

            var fileNameOnly = Path.GetFileNameWithoutExtension(viewModel.SelectedFile);
            var file = $"{viewModel.OutputFolder}\\{fileNameOnly}_{suffix}";

            if (!File.Exists(file))
            {
                // exception: while creating files
                try
                {
                    File.Create(file).Dispose();
                }
                catch (Exception ex)
                {
                    mainWindow.ShowMessage($"[ ERROR ] {ex.Message}");
                    return "";
                }
            }

            // cehck: output file is not locked
            if (IsFileLocked(file))
            {
                mainWindow.ShowMessage($"[ ERROR ] {file} is locked");
                return "";
            }

            return file;
        }
    }
}
