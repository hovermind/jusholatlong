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
using System.Collections.Generic;
using System;
using GoogleMaps.LocationServices;
using System.Threading;

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

        string selectedFileName = "";
        string outputFolder = "";
        string validAddressCsvFile = "";
        string missingAdressCsvFIle = "";

        // to hold found & missing addresses
        List<CompanyProfile> profilesWithCoordinate = null;
        List<CompanyProfile> profilesWithMissingAddress = null;

        public MainWindow() {
            InitializeComponent();

            fileUtil = new FileUtil();
            SetDefaultOutputFolder();

            profilesWithCoordinate = new List<CompanyProfile>();
            profilesWithMissingAddress = new List<CompanyProfile>();
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

        private async void OnClickGetMapCoordinate(object sender, RoutedEventArgs e) {

            // check file exists
            if (File.Exists(selectedFileName) == true) {
                // check that file is not locked (used by other programs i.e. excel)

                var isDataReady = await CreateOutputDataListAsync(this, selectedFileName);

                // write to file
                UpdateStatus("Writing csv... ...");
                if (isDataReady) {
                    var isWritingDone = await WriteDataToCsv(outputFolder);
                    if (isWritingDone) {
                        UpdateStatus("All done!");
                    }
                }
            }
        }

        async Task<bool> CreateOutputDataListAsync(MainWindow gui, string csvFile) {

            using (CsvReader csvReader = new CsvReader(new StreamReader(csvFile, Encoding.Default))) {

                csvReader.Configuration.Delimiter = ",";              // using "," instead of ";"
                csvReader.Configuration.HasHeaderRecord = false;      // can not map Japanese character to english property name
                csvReader.Configuration.MissingFieldFound = null;     // some field can be missing

                await Task.Run(async () => {

                    var rowCounter = 0;
                    var successCounter = 0;
                    var errorCounter = 0;

                    var locationService = new GoogleLocationService();
                    MapPoint mapPoint = null;

                    csvReader.Read();             // skip header (1st line)
                    while (csvReader.Read()) {

                        var profile = csvReader.GetRecord<CompanyProfile>();
                        var address = String.IsNullOrEmpty(profile.Address) ? "xbsjhUDFGUWEF78R7YT 8924512 FHGSDFG7" : profile.Address;
                        gui.UpdateStatus($"Getting co-ordinate...");

                        //Thread.Sleep(1000);
                        await Task.Delay(1000);

                        mapPoint = locationService.GetLatLongFromAddress(address);
                        if (mapPoint != null && mapPoint.Latitude != 0.0 && mapPoint.Longitude != 0.0) {

                            profile.CoordinateFlag = "1";
                            profile.Latitude = mapPoint.Latitude.ToString();
                            profile.Longitude = mapPoint.Longitude.ToString();

                            // add to list
                            profilesWithCoordinate.Add(profile);

                            // update gui
                            gui.UpdateStatus($"{profile.Latitude} , {profile.Longitude} [  {address}  ]");
                            gui.UpdateSuccess($"{++successCounter}");

                        } else {

                            // add to list
                            profilesWithMissingAddress.Add(profile);

                            // update gui
                            gui.UpdateStatus($"Not found");
                            gui.UpdateError($"{++errorCounter}");
                        }

                        //Thread.Sleep(1000);
                        await Task.Delay(1000);

                        if (++rowCounter > 10) {
                            break;
                        }
                    }
                });

                return true;
            }
        }

        private async Task<bool> WriteDataToCsv(string outputFolder) {
            validAddressCsvFile = $"{outputFolder}\\valid_address_with_coordinate.csv";
            missingAdressCsvFIle = $"{outputFolder}\\missing_address.csv";

            await Task.Run(() => {

                if (File.Exists(validAddressCsvFile) == true) {
                    using (var csvWriter = new CsvWriter(new StreamWriter(validAddressCsvFile))) {
                        csvWriter.WriteRecords(profilesWithCoordinate);
                    }
                } else {
                    using (var csvWriter = new CsvWriter(new StreamWriter(File.Create(validAddressCsvFile)))) {
                        csvWriter.WriteRecords(profilesWithCoordinate);
                    }
                }


                if (File.Exists(missingAdressCsvFIle) == true) {
                    using (var csvWriter = new CsvWriter(new StreamWriter(missingAdressCsvFIle))) {
                        csvWriter.WriteRecords(profilesWithMissingAddress);
                    }
                } else {
                    using (var csvWriter = new CsvWriter(new StreamWriter(File.Create(missingAdressCsvFIle)))) {
                        csvWriter.WriteRecords(profilesWithMissingAddress);
                    }
                }

            });

            return true;
        }

        // helper functions
        private void SetDefaultOutputFolder() {
            // If the folder does not exist yet, it will be created.
            // If the folder exists already, the line will be ignored.
            outputFolder = Directory.CreateDirectory(@"C:\CSV_Exported").FullName;
        }

        void UpdateStatus(string text) {
            //safe call
            Dispatcher.Invoke(() => {
                label_status_live_update.Content = text;
            });
        }

        void UpdateSuccess(string text) {
            //safe call
            Dispatcher.Invoke(() => {
                label_success_count.Content = text;
            });
        }

        void UpdateError(string text) {
            //safe call
            Dispatcher.Invoke(() => {
                label_error_count.Content = text;
            });
        }

        void Dlog(string tag, string msg) {
            Debug.WriteLine($"Logging from: {tag} => {msg}");
        }
    }
}
