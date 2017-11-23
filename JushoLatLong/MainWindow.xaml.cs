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
using static GoogleMaps.LocationServices.Directions;
using JushoLatLong.ViewModel;

namespace JushoLatLong {

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

        // activity status
        UiInfo uiInfo = null;

        public MainWindow() {
            InitializeComponent();

            uiInfo = new UiInfo();
            DataContext = uiInfo;

            Init();
        }

        private void Init() {

            fileUtil = new FileUtil();

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

        private void OnClickBrowseFileButton(object sender, RoutedEventArgs e) {
            isFileBrowserLaunched = true;
            ResetGuiTxt();

            // get selected file name
            selectedFileName = fileUtil?.GetSelectedFile("csv");

            // set selected file name to textbox
            if (String.IsNullOrEmpty(selectedFileName)) {

                UpdateStatus("Please select csv file");

                _TextBoxFileName.Text = "";
                _TextBoxOutputFolder.Text = "";

                // disable btn_get_map_cordinate
                DisableGetLatLongBtn();
            } else {

                _TextBoxFileName.Text = selectedFileName;
                UpdateStatus("");

                // enable btn_get_map_cordinate
                EnableGetLatLongBtn();

                // default output folder
                SetDefaultOutputFolder();
            }

        }

        private void OnClickBrowseFolderButton(object sender, RoutedEventArgs e) {
            outputFolder = fileUtil?.GetOutputFolder();

            // if user canceled folder selection
            if ("".Equals(outputFolder)) SetDefaultOutputFolder();

            // set output folder name to textbox
            _TextBoxOutputFolder.Text = outputFolder;
        }

        private async void OnClickGetMapCoordinateButton(object sender, RoutedEventArgs e) {
            // reset gui txt
            ResetGuiTxt();

            // check map api key
            var givenMapApiKey = _TextBoxMapApiKey.Text;
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

        private void OnClickStopApiCallButton(object sender, RoutedEventArgs e) {
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

                    csvReader.Read();             // 1st line == header

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
                            await Task.Delay(100);

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
            var fileNameOnly = Path.GetFileNameWithoutExtension(selectedFileName);
            validAddressCsvFile = $"{outputFolder}\\{fileNameOnly}_ok.csv";
            missingAdressCsvFIle = $"{outputFolder}\\{fileNameOnly}_error.csv";

            await Task.Run(() => {

                // valid addresses
                if (!File.Exists(validAddressCsvFile)) {
                    using (var fs = File.Create(validAddressCsvFile)) {
                        // auto disposal
                    }
                }
                using (var csvWriter = new CsvWriter(new StreamWriter(File.Open(validAddressCsvFile, FileMode.Truncate, FileAccess.ReadWrite)))) {
                    csvWriter.WriteRecord<CompanyProfile>(headers);
                    csvWriter.NextRecord();
                    csvWriter.WriteRecords(profilesWithCoordinate);
                }

                // missing addresses
                if (!File.Exists(missingAdressCsvFIle)) {
                    using (var fs = File.Create(missingAdressCsvFIle)) {
                        // auto disposal
                    }
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
            if (String.IsNullOrEmpty(selectedFileName)) {
                return;
            }

            var selectedFileDir = Path.GetDirectoryName(selectedFileName);
            var defaultOutputFolder = !string.IsNullOrEmpty(selectedFileDir) ? $"{selectedFileDir}\\CSV_Exported" : @"C:\CSV_Exported";

            try {
                // If the folder does not exist yet, it will be created.
                // If the folder exists already, the line will be ignored.
                outputFolder = Directory.CreateDirectory(defaultOutputFolder).FullName;

                //throw new UnauthorizedAccessException();
                _TextBoxOutputFolder.Text = outputFolder;

            } catch (Exception ex) {
                MessageBox.Show(caption: "Default Output Folder Error",
                                                   messageBoxText: $"Could not create defualt output folder \"{defaultOutputFolder}\" [ Error: {ex.Message} ]. Please browse and select output folder before calling API.",
                                                   button: MessageBoxButton.OK
                                              );
            }
        }

        private void UpdateStatus(string text) {
            //safe call
            Dispatcher.Invoke(() => {
                uiInfo.StatusMessage = text;
            });
        }

        private void UpdateSuccess(string text) {
            //safe call
            Dispatcher.Invoke(() => {
                _TextBoxSuccessCount.Text = text;
            });
        }

        private void UpdateError(string text) {
            //safe call
            Dispatcher.Invoke(() => {
                _TextBoxErrorCount.Text = text;
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
            _ButtonGetMapCoordinate.IsEnabled = true;
        }

        private void DisableGetLatLongBtn() {
            _ButtonGetMapCoordinate.IsEnabled = false;
        }

        private void DisableStopApiCallBtn() {
            _ButtonStopApiCall.IsEnabled = false;
        }

        private void EnableStopApiCallBtn() {
            _ButtonStopApiCall.IsEnabled = true;
        }

        private void DisableApiCallLatLongBtns() {
            DisableGetLatLongBtn();
            DisableStopApiCallBtn();
        }

        private void Dlog(string tag, string msg) {
            Debug.WriteLine($"Logging from: {tag} => {msg}");
        }

        private void OnChangeFileName(object sender, System.Windows.Controls.TextChangedEventArgs e) {

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
