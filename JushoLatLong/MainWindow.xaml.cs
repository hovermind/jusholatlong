using JushoLatLong.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace JushoLatLong
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        IFileUtil fileUtil = null;
        string selectedFileName = "";
        string outputFolder = "";
        string defaultOutputFolder = @"C:\CSV_Exported";

        public MainWindow()
        {
            InitializeComponent();

            fileUtil = new FileUtil();
        }

        private void OnClickBrowseFileBtn(object sender, RoutedEventArgs e)
        {
            // get selected file name
            selectedFileName = fileUtil?.GetSelectedFile("csv");

            // set selected file name to textbox
            if (!"".Equals(selectedFileName))
            {
                tb_file_name.Text = selectedFileName;

                // enable btn_load_map_data
            }
            else {
                tb_file_name.Text = "please choose a valid file!";

                // disable btn_load_map_data

            }

        }

        private void OnClickBrowseFolderBtn(object sender, RoutedEventArgs e)
        {
            outputFolder = fileUtil?.GetOutputFolder();

            // if user did not select folder, set default location
            if ("".Equals(outputFolder))
            {
                // If the folder does not exist yet, it will be created.
                // If the folder exists already, the line will be ignored.
                outputFolder = Directory.CreateDirectory(defaultOutputFolder).FullName;
            }

            // set output folder name to textbox
            tb_output_folder.Text = outputFolder;
        }

        private void OnClickGetMapCoordinate(object sender, RoutedEventArgs e)
        {

        }
    }
}
