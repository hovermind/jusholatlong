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
using System.Windows.Controls;
using System.Net;

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
        bool isFileBrowserLaunched = false;
        bool isApiKeyOrQueryLimitException = false;

        CancellationTokenSource cancellationTokenSource = null;
        string mapApiKey = "";
        string nonceAddress = "UDFGUWEF78R7YT 8924512";

        string selectedFileName = "";
        string outputFolder = "";
        string validAddressCsvFile = "";
        string missingAdressCsvFIle = "";

        // to hold found & missing addresses
        List<CompanyProfile> profilesWithCoordinate = null;
        List<CompanyProfile> profilesWithMissingAddress = null;
        CompanyProfile headers = null;

        public MainWindow() {
            InitializeComponent();

            Init();
        }

        private void Init() {

            fileUtil = new FileUtil();

            // default output folder
            SetDefaultOutputFolder();
            tb_output_folder.Text = outputFolder;

            headers = new CompanyProfile {
                CompanyCode = "会社コード",
                PrefecturesName = "都道府県名",
                CityName = "市区町村名",
                Address = "以下住所",
                Remarks = "備考",
                Registrant = "登録者",
                RegistrationDate = "登録日",
                Apo = "APO",
                RecordNumber = "レコード番号",
                Latitude = "緯度",
                Longitude = "経度"
            };
        }

        private void OnClickBrowseFileBtn(object sender, RoutedEventArgs e) {
            isFileBrowserLaunched = true;
            ResetGuiTxt();

            // get selected file name
            selectedFileName = fileUtil?.GetSelectedFile("csv");

            // set selected file name to textbox
            if (String.IsNullOrEmpty(selectedFileName)) {

                tb_file_name.Text = "";
                // disable btn_get_map_cordinate
                DisableGetLatLongBtn();

                UpdateStatus("Please select csv file");

            } else {

                tb_file_name.Text = selectedFileName;
                // enable btn_get_map_cordinate
                EnableGetLatLongBtn();

                UpdateStatus("");
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
            // reset gui txt
            ResetGuiTxt();

            // check map api key
            var givenMapApiKey = tb_map_api_key.Text;
            if (String.IsNullOrEmpty(givenMapApiKey)) {

                var dialogResult = MessageBox.Show(caption: "Google Map API Key",
                                                   messageBoxText: "You must provide Google Map API Key.",
                                                   button: MessageBoxButton.OK
                                              );
                return;

            } else {
                mapApiKey = givenMapApiKey;
            }

            // check file exists
            if (File.Exists(selectedFileName) == false) {

                // file does not exist
                UpdateStatus("File does not exist, please select again!");

                DisableGetLatLongBtn();
                return;

            } else {

                // check that file is not locked (used by other programs i.e. Excel)
                if (fileUtil.IsFileLocked(selectedFileName)) {

                    // file is locked
                    UpdateStatus("File is locked, please close it & try again!");

                    DisableGetLatLongBtn();
                    return;

                } else {

                    DisableGetLatLongBtn();
                    EnableStopApiCallBtn();

                    // prepare data lists
                    var isDataReady = await CreateOutputDataListAsync(this, selectedFileName);

                    if (!cancellationTokenSource.IsCancellationRequested) {

                        if (isApiKeyOrQueryLimitException) {

                            isApiKeyOrQueryLimitException = false;

                            var dialogResult = MessageBox.Show(caption: "Exception!",
                                                               messageBoxText: "Wrong map API key or query limit exceeded!",
                                                               button: MessageBoxButton.OK
                                                          );
                            EnableGetLatLongBtn();
                            DisableStopApiCallBtn();

                            return;

                        } else {

                            // export data to csv
                            ExportDataToCsv(isDataReady);
                        }

                    } else {

                        var dialogResult = MessageBox.Show(caption: "API call cancelled",
                                                           messageBoxText: "Would you like to write data to csv?",
                                                           button: MessageBoxButton.YesNo
                                                      );

                        if (dialogResult == MessageBoxResult.Yes) {

                            ExportDataToCsv(true);

                        } else if (dialogResult == MessageBoxResult.No) {

                            ResetGuiTxt();
                            UpdateStatus("API call cancelled!");

                            ResetDataList();

                            EnableGetLatLongBtn();
                            DisableStopApiCallBtn();
                        }
                    }
                }
            }
        }

        private void OnClickStopApiCallBtn(object sender, RoutedEventArgs e) {
            cancellationTokenSource?.Cancel();
        }

        private async Task<bool> CreateOutputDataListAsync(MainWindow gui, string csvFile) {

            profilesWithCoordinate = new List<CompanyProfile>();
            profilesWithMissingAddress = new List<CompanyProfile>();

            using (cancellationTokenSource = new CancellationTokenSource())
            using (CsvReader csvReader = new CsvReader(new StreamReader(csvFile, Encoding.Default))) {

                var apiCallCancelTonek = cancellationTokenSource.Token;

                csvReader.Configuration.Delimiter = ",";              // using "," instead of ";"
                csvReader.Configuration.HasHeaderRecord = false;      // can not map Japanese character to english property name
                csvReader.Configuration.MissingFieldFound = null;     // some field can be missing

                await Task.Run(async () => {

                    var rowCounter = 0;
                    var successCounter = 0;
                    var errorCounter = 0;

                    var locationService = new GoogleLocationService(mapApiKey);
                    MapPoint mapPoint = null;

                    csvReader.Read();             // skip header (1st line)
                    while (csvReader.Read()) {
                        if (apiCallCancelTonek.IsCancellationRequested) {
                            break;
                        }
                        if (++rowCounter > 10) {
                            break;
                        }

                        var profile = csvReader.GetRecord<CompanyProfile>();
                        var address = String.IsNullOrEmpty(profile.Address) ? nonceAddress : profile.Address;

                        try {

                            mapPoint = locationService.GetLatLongFromAddress(address);
                            if (mapPoint != null && mapPoint.Latitude != 0.0 && mapPoint.Longitude != 0.0) {

                                profile.Latitude = mapPoint.Latitude.ToString();
                                profile.Longitude = mapPoint.Longitude.ToString();

                                // add to valid address list
                                profilesWithCoordinate.Add(profile);

                                // update gui
                                gui.UpdateStatus($"{profile.Latitude} , {profile.Longitude} [  {address}  ]");
                                gui.UpdateSuccess($"{++successCounter}");

                            } else {

                                // add to missing address list
                                profilesWithMissingAddress.Add(profile);

                                // update gui
                                gui.UpdateStatus("Not found");
                                gui.UpdateError($"{++errorCounter}");
                            }

                            //Thread.Sleep(1000);
                            await Task.Delay(1000);

                        } catch (WebException ex) {

                            isApiKeyOrQueryLimitException = true;

                            // update gui
                            UpdateStatus($"[ ERROR: {ex.Message} ]");

                            break;
                        }
                    }
                });

                return true;
            }
        }

        private async void ExportDataToCsv(bool isDataReady) {

            if (isDataReady) {
                DisableApiCallLatLongBtns();

                // write data to csv file
                UpdateStatus("Writing csv  . . .    . . .");
                await Task.Delay(1500);

                var isWritingDone = await WriteDataToCsvAsync(outputFolder);

                if (isWritingDone) {
                    //ResetGuiTxt();
                    UpdateStatus("All done!");
                    EnableGetLatLongBtn();
                }
            }
        }

        private async Task<bool> WriteDataToCsvAsync(string outputFolder) {

            validAddressCsvFile = $"{outputFolder}\\valid_address_with_coordinate.csv";
            missingAdressCsvFIle = $"{outputFolder}\\missing_address.csv";

            await Task.Run(() => {

                // valid addresses
                if (!File.Exists(validAddressCsvFile)) {
                    File.Create(validAddressCsvFile);
                }
                using (var csvWriter = new CsvWriter(new StreamWriter(File.Open(validAddressCsvFile, FileMode.Truncate, FileAccess.ReadWrite)))) {
                    csvWriter.WriteRecord<CompanyProfile>(headers);
                    csvWriter.NextRecord();
                    csvWriter.WriteRecords(profilesWithCoordinate);
                }

                // missing addresses
                if (!File.Exists(missingAdressCsvFIle)) {
                    File.Create(missingAdressCsvFIle);
                }
                using (var csvWriter = new CsvWriter(new StreamWriter(File.Open(missingAdressCsvFIle, FileMode.Truncate, FileAccess.ReadWrite)))) {
                    csvWriter.WriteRecord<CompanyProfile>(headers);
                    csvWriter.NextRecord();
                    csvWriter.WriteRecords(profilesWithMissingAddress);
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

        private void UpdateStatus(string text) {
            //safe call
            Dispatcher.Invoke(() => {
                label_status_live_update.Content = text;
            });
        }

        private void UpdateSuccess(string text) {
            //safe call
            Dispatcher.Invoke(() => {
                label_success_count.Content = text;
            });
        }

        private void UpdateError(string text) {
            //safe call
            Dispatcher.Invoke(() => {
                label_error_count.Content = text;
            });
        }

        private void ResetGuiTxt() {
            UpdateStatus("");
            UpdateSuccess("0");
            UpdateError("0");
        }

        private void ResetDataList() {
            profilesWithCoordinate = null;
            profilesWithMissingAddress = null;
        }

        private void EnableGetLatLongBtn() {
            btn_get_map_coordinate.IsEnabled = true;
        }

        private void DisableGetLatLongBtn() {
            btn_get_map_coordinate.IsEnabled = false;
        }

        private void DisableStopApiCallBtn() {
            btn_stop_api_call.IsEnabled = false;
        }

        private void EnableStopApiCallBtn() {
            btn_stop_api_call.IsEnabled = true;
        }

        private void DisableApiCallLatLongBtns() {
            DisableGetLatLongBtn();
            DisableStopApiCallBtn();
        }

        private void Dlog(string tag, string msg) {
            Debug.WriteLine($"Logging from: {tag} => {msg}");
        }

        private void OnFileNameTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) {
            
            // do not listen if file browser launched
            if (isFileBrowserLaunched) {
                isFileBrowserLaunched = false;
                return;
            }

            var strFileName = (sender as TextBox)?.Text;

            if (File.Exists(strFileName)) {
                selectedFileName = strFileName;
                UpdateStatus("");
                EnableGetLatLongBtn();
            } else {
                UpdateStatus("File does not exist! Please select valid file");
                DisableGetLatLongBtn();
            }
        }
    }
}
