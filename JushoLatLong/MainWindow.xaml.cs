
using JushoLatLong.Model.Domain;
using JushoLatLong.Utils;
using System.IO;
using System.Windows;
using CsvHelper;
using System.Text;
using System.Threading.Tasks;
using System;
using GoogleMaps.LocationServices;
using System.Threading;
using System.Net;
using JushoLatLong.ViewModel;

namespace JushoLatLong
{

    public partial class MainWindow : Window
    {
        // data context
        private ActivityViewModel activity = null;

        // api call cancellation token
        private CancellationTokenSource cts = null;

        private IFileUtil fileUtil = null;

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
            DisableCallApiButton();
            EnableStopApiButton();

            #region Map API Key

            // check: map api key is provided
            if (String.IsNullOrEmpty(activity.MapApiKey))
            {
                ShowMessage("Please provide Google Map API Key");
                EnableCallApiButton();
                DisableStopApiButton();

                return;
            }

            // check: map api key is valid & did not exceed query limit not
            try
            {
                var point = new GoogleLocationService(activity.MapApiKey).GetLatLongFromAddress("1 Chome-9 Marunouchi, Chiyoda-ku, Tōkyō-to 100-0005");
            }
            catch (WebException ex)
            {
                ShowMessage("Invalid Map API Key or query limit exceeded!");
                EnableCallApiButton();
                DisableStopApiButton();

                return;
            }

            #endregion

            #region Input CSV File

            // check input csv file exists & not locked
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

                EnableCallApiButton();
                DisableStopApiButton();

                return;
            }

            #endregion

            #region Output CSV File

            // ouput folder
            if (String.IsNullOrEmpty(activity.OutputFolder)) SetDefaultOutputFolder(activity.SelectedFile);

            // exception: while creating files
            var fileNameOnly = Path.GetFileNameWithoutExtension(activity.SelectedFile);
            var validAddressCsvFile = $"{activity.OutputFolder}\\{fileNameOnly}_ok.csv";
            var missingAdressCsvFIle = $"{activity.OutputFolder}\\{fileNameOnly}_error.csv";
            try
            {
                if (!File.Exists(validAddressCsvFile)) File.Create(validAddressCsvFile).Close();
                if (!File.Exists(missingAdressCsvFIle)) File.Create(missingAdressCsvFIle).Close();
            }
            catch (Exception ex)
            {
                ShowMessage($"[ ERROR ] {ex.Message}");
                EnableCallApiButton();
                DisableStopApiButton();

                return;
            }

            // cehck: output file are not locked
            if (fileUtil.IsFileLocked(validAddressCsvFile) || fileUtil.IsFileLocked(missingAdressCsvFIle))
            {
                ShowMessage($"[ ERROR ] output csv file locked");
                EnableCallApiButton();
                DisableStopApiButton();

                return;
            }

            #endregion

            // cancellation TokenSource
            cts?.Dispose();
            cts = new CancellationTokenSource();

            // api call related exception
            var isWebException = false;

            using (var csvReader = new CsvReader(new StreamReader(activity.SelectedFile, Encoding.UTF8)))
            using (var okCsvWriter = new CsvWriter(new StreamWriter(File.Open(validAddressCsvFile, FileMode.Truncate, FileAccess.ReadWrite))))
            using (var errorCsvWriter = new CsvWriter(new StreamWriter(File.Open(missingAdressCsvFIle, FileMode.Truncate, FileAccess.ReadWrite))))
            {
                // reader configuration
                csvReader.Configuration.Delimiter = ",";              // using "," instead of ";"
                csvReader.Configuration.HasHeaderRecord = false;      // can not map Japanese character to english property name
                csvReader.Configuration.MissingFieldFound = null;     // some field can be missing

                await Task.Run(async () =>
                {

                    //var rowCounter = 0;
                    var successCounter = 0;
                    var errorCounter = 0;

                    // write headers
                    okCsvWriter.WriteRecord(headers);
                    okCsvWriter.NextRecord();
                    errorCsvWriter.WriteRecord(headers);
                    errorCsvWriter.NextRecord();

                    var locationService = new GoogleLocationService(activity.MapApiKey);
                    MapPoint mapPoint = null;

                    // start reading
                    csvReader.Read();   // skip header
                    while (csvReader.Read())
                    {
                        if (cts.Token.IsCancellationRequested) break;

                        var profile = csvReader.GetRecord<CompanyProfile>();
                        var address = profile?.Address;

                        // in case address is null or empty
                        if (String.IsNullOrEmpty(address))
                        {
                            errorCsvWriter.WriteRecord(profile);
                            errorCsvWriter.NextRecord();

                            // update gui
                            ShowMessage("Address is null or empty");
                            UpdateErrorCount($"{++errorCounter}");

                            continue;
                        }

                        try
                        {
                            mapPoint = locationService.GetLatLongFromAddress(address);
                            if (mapPoint != null && mapPoint.Latitude != 0.0 && mapPoint.Longitude != 0.0)
                            {
                                profile.Latitude = mapPoint.Latitude.ToString();
                                profile.Longitude = mapPoint.Longitude.ToString();

                                okCsvWriter.WriteRecord(profile);
                                okCsvWriter.NextRecord();

                                // update gui
                                ShowMessage($"{profile.Latitude} , {profile.Longitude} [  {address}  ]");
                                UpdateSuccessCount($"{++successCounter}");
                            }
                            else
                            {
                                errorCsvWriter.WriteRecord(profile);
                                errorCsvWriter.NextRecord();

                                // update gui
                                ShowMessage("Not found");
                                UpdateErrorCount($"{++errorCounter}");
                            }

                            await Task.Delay(100); // Google Map API call limit: 25 calls / sec.
                        }
                        catch (WebException ex)
                        {
                            isWebException = true;
                            // update gui
                            ShowMessage($"[ ERROR ] {ex?.Message}");
                            return;
                        }
                    }
                });

                EnableCallApiButton();
                DisableStopApiButton();

                if (isWebException) return;

                ShowMessage("All done");
                if (cts.Token.IsCancellationRequested) ShowMessage("All done (Api call cancelled)");
            }
        }

        private void OnClickStopApiButton(object sender, RoutedEventArgs e)
        {
            cts?.Cancel();
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
                MessageBox.Show(caption: "Output Folder Error",
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
