using JushoLatLong.Controller;
using JushoLatLong.Model.Domain;
using JushoLatLong.Utils;
using System.IO;
using System.Windows;
using System.Diagnostics;
using CsvHelper;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace JushoLatLong {

    public enum ProfileHeader : int {
        CompanyCode = 0,
        PrefecturesName = 1,
        CityName = 2,
        Address = 3,
        Remarks = 4,
        Registrant = 5,
        RegistrationDate = 6,
        Apo = 7,
        RecordNumber = 8
    }

    public partial class MainWindow : Window {
        IFileUtil fileUtil = null;
        //ICsvController csvController = null;

        string selectedFileName = "";
        string outputFolder = "";
        string defaultOutputFolder = @"C:\CSV_Exported";

        private void SetDefaultOutputFolder() {
            // set default location
            if ("".Equals(outputFolder)) {
                // If the folder does not exist yet, it will be created.
                // If the folder exists already, the line will be ignored.
                outputFolder = Directory.CreateDirectory(defaultOutputFolder).FullName;
            }
        }

        public MainWindow() {
            InitializeComponent();

            fileUtil = new FileUtil();
            //csvController = new CsvController();
            SetDefaultOutputFolder();
        }

        private void OnClickBrowseFileBtn(object sender, RoutedEventArgs e) {
            // get selected file name
            selectedFileName = fileUtil?.GetSelectedFile("csv");

            // set selected file name to textbox
            if (!"".Equals(selectedFileName)) {
                tb_file_name.Text = selectedFileName;
                // enable btn_get_map_cordinate
                btn_get_map_coordinate.IsEnabled = true;
            } else {
                tb_file_name.Text = "please choose a valid file!";
                // disable btn_get_map_cordinate
                btn_get_map_coordinate.IsEnabled = false;
            }

        }

        private void OnClickBrowseFolderBtn(object sender, RoutedEventArgs e) {
            outputFolder = fileUtil?.GetOutputFolder();

            // if user canceled folder selection
            if ("".Equals(outputFolder)) SetDefaultOutputFolder();

            // set output folder name to textbox
            tb_output_folder.Text = outputFolder;
        }

        private void OnClickGetMapCoordinate(object sender, RoutedEventArgs e) {

            if (File.Exists(selectedFileName) == true) {

                CreateOutputDataList(this, selectedFileName);
            }
        }

        void UpdateStatus(string text) {
            //safe call
            Dispatcher.Invoke(() => {
                label_status_live_update.Content = text;
            });
        }

        async void CreateOutputDataList(MainWindow gui, string csvFile) {

            using (CsvReader csv = new CsvReader(new StreamReader(csvFile, Encoding.Default))) {

                csv.Configuration.Delimiter = ",";              // using "," instead of ";"
                csv.Configuration.HasHeaderRecord = false;      // can not map Japanese character to english property name

                // some field can be missing
                csv.Configuration.MissingFieldFound = (headerNames, index, context) => {
                    Debug.WriteLine($"Field with names ['{string.Join("', '", headerNames)}'] at index '{index}' was not found.");
                };

                //var records = csv.GetRecords<T>();
                //Debug.WriteLine();

                csv.Read();             // skip header (1st line)

                await Task.Run(() => {
                    var rowCounter = 0;
                    var progress = "Reading csv file, please wait";
                    var dotCounter = 0;

                    while (csv.Read()) {

                        var profile = csv.GetRecord<CompanyProfile>();
                        Debug.WriteLine(profile.Address);

                        ++rowCounter;
                        if (rowCounter % 1000 == 0) {

                            ++dotCounter;

                            if (dotCounter > 3) {

                                dotCounter = 0;
                                progress = "Reading csv file, please wait";

                            } else {
                                for (int i = 0; i < dotCounter; i++) {
                                    progress += " . ";
                                }
                            }

                            gui.UpdateStatus(progress);
                        }

                    }

                    gui.UpdateStatus($"Read {rowCounter} rows");
                });


            }
        }

    }
}
