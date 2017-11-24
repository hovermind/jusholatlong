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

namespace JushoLatLong
{

    public partial class MainWindow : Window
    {

        // data context
        private ActivityViewModel activity = null;

        private IFileUtil fileUtil = null;
        private CancellationTokenSource cancellationTokenSource = null;

        bool isApiKeyOrQueryLimitException = false;



        string validAddressCsvFile = "";
        string missingAdressCsvFIle = "";

        // to hold found & missing addresses
        List<CompanyProfile> profilesWithCoordinate = null;
        List<CompanyProfile> profilesWithMissingAddress = null;
        CompanyProfile headers = null;


        public MainWindow()
        {
            InitializeComponent();

            // set data context
            activity = new ActivityViewModel();
            DataContext = activity;

            fileUtil = new FileUtil();

            headers = new CompanyProfile
            {
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

        private void OnClickBrowseFileButton(object sender, RoutedEventArgs e)
        {

            ResetUIMessages();

            // selected file (csv)
            var fileNameString = fileUtil?.GetSelectedFile("csv");
            activity.SelectedFile = fileNameString;

            if (!String.IsNullOrEmpty(fileNameString))
            {
                if (String.IsNullOrEmpty(activity.OutputFolder)) SetDefaultOutputFolder(fileNameString);
                ShowMessage("");
                EnableCallApiButton();

            }
            else
            {

                ShowMessage("Please select file");
                DisableCallApiButton();

            }

        }

        private void OnClickBrowseFolderButton(object sender, RoutedEventArgs e)
        {

            var outputFolderString = fileUtil?.GetOutputFolder();

            activity.OutputFolder = outputFolderString;

        }

        private async void OnClickCallApiButton(object sender, RoutedEventArgs e)
        {
            ResetUIMessages();

            // check map api key is provided
            if (String.IsNullOrEmpty(activity.MapApiKey))
            {
                ShowMessage("Please provide Google Map API Key");
                return;
            }

            // check file exists & not locked
            if (!File.Exists(activity.SelectedFile) || fileUtil.IsFileLocked(activity.SelectedFile))
            {
                if (!File.Exists(activity.SelectedFile))
                {
                    ShowMessage("File does not exist, please select again!");
                }
                else
                {
                    ShowMessage("File is locked, please close it & try again!");
                }

                DisableCallApiButton();
                return;
            }

            DisableCallApiButton();
            EnableStopApiButton();

            // prepare data lists
            var isDataReady = await CreateOutputDataListAsync(this, activity.SelectedFile);

            if (!cancellationTokenSource.IsCancellationRequested)
            {

                if (isApiKeyOrQueryLimitException)
                {

                    isApiKeyOrQueryLimitException = false;

                    var dialogResult = MessageBox.Show(caption: "Exception!",
                                                       messageBoxText: "Wrong map API key or query limit exceeded!",
                                                       button: MessageBoxButton.OK
                                                  );
                    EnableCallApiButton();
                    DisableStopApiButton();

                    return;

                }
                else
                {

                    // export data to csv
                    ExportDataToCsv(isDataReady);
                }

            }
            else
            {

                var dialogResult = MessageBox.Show(caption: "API call cancelled",
                                                   messageBoxText: "Would you like to write data to csv?",
                                                   button: MessageBoxButton.YesNo
                                              );

                if (dialogResult == MessageBoxResult.Yes)
                {

                    ExportDataToCsv(true);

                }
                else if (dialogResult == MessageBoxResult.No)
                {

                    ResetUIMessages();
                    ShowMessage("API call cancelled!");

                    ResetDataList();

                    EnableCallApiButton();
                    DisableStopApiButton();
                }
            }
        }

        private void OnClickStopApiButton(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource?.Cancel();
        }

        private async Task<bool> CreateOutputDataListAsync(MainWindow gui, string csvFile)
        {

            profilesWithCoordinate = new List<CompanyProfile>();
            profilesWithMissingAddress = new List<CompanyProfile>();

            using (cancellationTokenSource = new CancellationTokenSource())
            using (CsvReader csvReader = new CsvReader(new StreamReader(csvFile, Encoding.UTF8)))
            {

                var apiCallCancelTonek = cancellationTokenSource.Token;

                csvReader.Configuration.Delimiter = ",";              // using "," instead of ";"
                csvReader.Configuration.HasHeaderRecord = false;      // can not map Japanese character to english property name
                csvReader.Configuration.MissingFieldFound = null;     // some field can be missing

                await Task.Run(async () =>
                {

                    var rowCounter = 0;
                    var successCounter = 0;
                    var errorCounter = 0;

                    var locationService = new GoogleLocationService(activity.MapApiKey);
                    MapPoint mapPoint = null;

                    csvReader.Read();             // 1st line == header

                    while (csvReader.Read())
                    {
                        if (apiCallCancelTonek.IsCancellationRequested)
                        {
                            break;
                        }
                        if (++rowCounter > 10)
                        {
                            break;
                        }

                        var profile = csvReader.GetRecord<CompanyProfile>();
                        var address = String.IsNullOrEmpty(profile.Address) ? "UDFGUWEF78R7YT 8924512" : profile.Address;

                        try
                        {

                            mapPoint = locationService.GetLatLongFromAddress(address);
                            if (mapPoint != null && mapPoint.Latitude != 0.0 && mapPoint.Longitude != 0.0)
                            {

                                profile.Latitude = mapPoint.Latitude.ToString();
                                profile.Longitude = mapPoint.Longitude.ToString();

                                // add to valid address list
                                profilesWithCoordinate.Add(profile);

                                // update gui
                                gui.ShowMessage($"{profile.Latitude} , {profile.Longitude} [  {address}  ]");
                                gui.UpdateSuccessCount($"{++successCounter}");

                            }
                            else
                            {

                                // add to missing address list
                                profilesWithMissingAddress.Add(profile);

                                // update gui
                                gui.ShowMessage("Not found");
                                gui.UpdateErrorCount($"{++errorCounter}");
                            }

                            //Thread.Sleep(1000);
                            await Task.Delay(100);

                        }
                        catch (WebException ex)
                        {

                            isApiKeyOrQueryLimitException = true;

                            // update gui
                            ShowMessage($"[ ERROR: {ex.Message} ]");

                            break;
                        }
                    }
                });

                return true;
            }
        }

        private async void ExportDataToCsv(bool isDataReady)
        {

            if (isDataReady)
            {
                DisableAllButtons();

                // write data to csv file
                ShowMessage("Writing csv  . . .    . . .");
                await Task.Delay(1500);

                var isWritingDone = await WriteDataToCsvAsync(activity.OutputFolder);

                if (isWritingDone)
                {
                    //ResetGuiTxt();
                    ShowMessage("All done!");
                    EnableCallApiButton();
                }
            }
        }

        private async Task<bool> WriteDataToCsvAsync(string outputFolder)
        {
            var fileNameOnly = Path.GetFileNameWithoutExtension(activity.SelectedFile);
            validAddressCsvFile = $"{outputFolder}\\{fileNameOnly}_ok.csv";
            missingAdressCsvFIle = $"{outputFolder}\\{fileNameOnly}_error.csv";

            await Task.Run(() =>
            {

                // valid addresses
                if (!File.Exists(validAddressCsvFile))
                {
                    using (var fs = File.Create(validAddressCsvFile))
                    {
                        // auto disposal
                    }
                }
                using (var csvWriter = new CsvWriter(new StreamWriter(File.Open(validAddressCsvFile, FileMode.Truncate, FileAccess.ReadWrite))))
                {
                    csvWriter.WriteRecord<CompanyProfile>(headers);
                    csvWriter.NextRecord();
                    csvWriter.WriteRecords(profilesWithCoordinate);
                }

                // missing addresses
                if (!File.Exists(missingAdressCsvFIle))
                {
                    using (var fs = File.Create(missingAdressCsvFIle))
                    {
                        // auto disposal
                    }
                }
                using (var csvWriter = new CsvWriter(new StreamWriter(File.Open(missingAdressCsvFIle, FileMode.Truncate, FileAccess.ReadWrite))))
                {
                    csvWriter.WriteRecord<CompanyProfile>(headers);
                    csvWriter.NextRecord();
                    csvWriter.WriteRecords(profilesWithMissingAddress);
                }

            });

            return true;
        }

        #region Helper Functions

        private void SetDefaultOutputFolder(string selectedFile)
        {

            // early return
            if (String.IsNullOrEmpty(selectedFile))
            {
                activity.OutputFolder = "";
                return;
            }

            var outputFolderString = $"{Path.GetDirectoryName(selectedFile)}\\CSV_Exported";
            try
            {

                var defaulOutputFolder = Directory.CreateDirectory(outputFolderString).FullName;
                //throw new UnauthorizedAccessException();

                activity.OutputFolder = defaulOutputFolder;

            }
            catch (Exception ex)
            {
                MessageBox.Show(caption: "Default Output Folder Error",
                                 messageBoxText: $"Could not create defualt output folder \"{outputFolderString}\" [ Error: {ex.Message} ].\nPlease browse and select output folder before calling API.",
                                 button: MessageBoxButton.OK
                               );
            }
        }

        private void ShowMessage(string statusMessage)
        {
            activity.StatusMessage = statusMessage;
        }

        private void UpdateSuccessCount(string successCount)
        {
            activity.SuccessCount = successCount;
        }

        private void UpdateErrorCount(string errorCount)
        {
            activity.ErrorCount = errorCount;
        }

        private void ResetUIMessages()
        {
            ShowMessage("");
            UpdateSuccessCount("0");
            UpdateErrorCount("0");
        }

        private void ResetDataList()
        {
            profilesWithCoordinate = null;
            profilesWithMissingAddress = null;
        }

        private void EnableCallApiButton()
        {
            activity.IsEnabledCallApiButton = true;
        }

        private void DisableCallApiButton()
        {
            activity.IsEnabledCallApiButton = false;
        }

        private void EnableStopApiButton()
        {
            activity.IsEnabledStopApiButton = true;
        }

        private void DisableStopApiButton()
        {
            activity.IsEnabledStopApiButton = false;
        }

        private void DisableAllButtons()
        {
            DisableCallApiButton();
            DisableStopApiButton();
        }

        #endregion
    }
}
