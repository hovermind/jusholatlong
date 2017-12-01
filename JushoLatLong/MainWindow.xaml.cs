using JushoLatLong.Utils;
using System.IO;
using System.Windows;
using CsvHelper;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Threading;
using JushoLatLong.ViewModel;
using System.Windows.Controls;
using JushoLatLong.MapApi;
using JushoLatLong.Model.Api;

namespace JushoLatLong
{

    public partial class MainWindow : Window
    {
        // data context
        private ActivityViewModel ViewModel { get; set; }

        // api call cancellation token
        private CancellationTokenSource cts = null;

        // utils
        private IFileUtil fileUtil = null;
        private CommonUtil commonUtil = null;

        public MainWindow()
        {
            InitializeComponent();

            // set data context
            DataContext = ViewModel = new ActivityViewModel();

            fileUtil = new FileUtil();
            commonUtil = new CommonUtil();
        }

        public void OnClickBrowseFileButton(object sender, RoutedEventArgs e)
        {
            ResetUIMessages();

            // selected file (csv)
            var fileNameString = fileUtil?.GetSelectedFile("csv");
            ViewModel.SelectedFile = fileNameString;

            if (!String.IsNullOrEmpty(fileNameString))
            {
                if (String.IsNullOrEmpty(ViewModel.OutputFolder)) SetDefaultOutputFolder(fileNameString);
                ShowMessage("");
                EnableCallApiButton();
            }
            else
            {
                ShowMessage("Please select file");
                DisableCallApiButton();
            }
        }

        public void OnClickBrowseFolderButton(object sender, RoutedEventArgs e)
        {
            var outputFolderString = fileUtil?.GetOutputFolder();

            ViewModel.OutputFolder = outputFolderString;
        }

        public async void OnClickCallApiButton(object sender, RoutedEventArgs e)
        {
            ResetUIMessages();
            ToggleButtonState();

            // validate api key
            if (!GeocodingService.IsApiKeyValid(ViewModel.MapApiKey))
            {
                ShowErrorMessage("The provided API key is invalid.");
                ToggleButtonState();
                return;
            }

            // validate input file
            if (!fileUtil.IsFileOkToRead(ViewModel.SelectedFile))
            {
                ShowErrorMessage("Input file - locked or does not exist");
                ToggleButtonState();
                return;
            }

            // prepare ouput files
            try { PrepareOutputFiles(); }
            catch (Exception ex)
            {
                ShowErrorMessage($"{ex.Message}");
                ToggleButtonState();
                return;
            }
            if (!fileUtil.IsFileOkToWrite(ViewModel.OkOutputFileUri) || !fileUtil.IsFileOkToWrite(ViewModel.ErrorOutputFileUri))
            {
                ShowErrorMessage("Output files - could not create or readonly or locked");
                ToggleButtonState();
                return;
            }

            // cancellation TokenSource
            cts?.Dispose();
            cts = new CancellationTokenSource();

            using (var csvReader = new CsvReader(new StreamReader(ViewModel.SelectedFile, Encoding.UTF8)))
            using (var validCsvWriter = new CsvWriter(new StreamWriter(File.Open(ViewModel.OkOutputFileUri, FileMode.Truncate, FileAccess.ReadWrite), Encoding.UTF8)))
            using (var errorCsvWriter = new CsvWriter(new StreamWriter(File.Open(ViewModel.ErrorOutputFileUri, FileMode.Truncate, FileAccess.ReadWrite), Encoding.UTF8)))
            {
                #region Csv Reader & Writer Configurations

                // reader configuration
                csvReader.Configuration.Encoding = Encoding.UTF8;
                csvReader.Configuration.Delimiter = ",";                    // using "," instead of ";"
                csvReader.Configuration.HasHeaderRecord = false;            // can not map Japanese character to english property name
                csvReader.Configuration.MissingFieldFound = null;           // some field can be missing

                // writer configuration
                validCsvWriter.Configuration.QuoteAllFields = true;
                validCsvWriter.Configuration.Delimiter = ",";
                validCsvWriter.Configuration.HasHeaderRecord = false;       // for non-english header (first record instead of header record)
                errorCsvWriter.Configuration.QuoteAllFields = true;
                errorCsvWriter.Configuration.Delimiter = ",";
                errorCsvWriter.Configuration.HasHeaderRecord = false;       // for non-english header (first record instead of header record)

                #endregion

                await Task.Run(async () =>
                {
                    //var rowCounter = 0;
                    var successCounter = 0;
                    var errorCounter = 0;

                    var apiService = new GeocodingService(ViewModel.MapApiKey);
                    GeoPoint mapPoint = null;

                    #region Writing Header

                    csvReader.Read();
                    var headers = csvReader.GetRecord<dynamic>();

                    // error csv file does not need extra column
                    errorCsvWriter.WriteRecord(headers);
                    errorCsvWriter.NextRecord();

                    // ok csv file needs 2 extra column
                    if (ViewModel.IsHeaderJP)
                    {
                        headers.Latitude = "緯度";    // ido
                        headers.Longitude = "経度";   // keido
                    }
                    else
                    {
                        headers.Latitude = "Latitude";
                        headers.Longitude = "Longitude";
                    }
                    validCsvWriter.WriteRecord(headers);
                    validCsvWriter.NextRecord();

                    #endregion

                    while (csvReader.Read())
                    {
                        if (cts.Token.IsCancellationRequested) return;

                        var profile = csvReader.GetRecord<dynamic>();                // entire row
                        var address = csvReader[ViewModel.AddressColumnIndex - 1];   // address column

                        // address is null or empty
                        if (String.IsNullOrEmpty(address))
                        {
                            errorCsvWriter.WriteRecord(profile);
                            errorCsvWriter.NextRecord();
                            ShowError("Address is null or empty", ++errorCounter);
                            continue;
                        }

                        try
                        {
                            mapPoint = await apiService.GetGeoPointAsync(address);

                            if (mapPoint.Latitude != 0.0 && mapPoint.Longitude != 0.0)
                            {
                                profile.Latitude = mapPoint.Latitude.ToString();
                                profile.Longitude = mapPoint.Longitude.ToString();

                                validCsvWriter.WriteRecord(profile);
                                validCsvWriter.NextRecord();
                                ShowSuccess($"{profile.Latitude} , {profile.Longitude} [  {address}  ]", ++successCounter);
                            }
                            else
                            {
                                errorCsvWriter.WriteRecord(profile);
                                errorCsvWriter.NextRecord();
                                ShowError("Wrong address", ++errorCounter);
                            }

                            //await Task.Delay(50); // Google Map API call limit: 25 calls / sec.
                        }
                        catch (GeocodingException ex)
                        {
                            ShowMessage($"[ Exception ] {ex.Message}");
                            ToggleButtonState();
                            return;
                        }
                    }
                });

                ToggleButtonState();
                if (cts.Token.IsCancellationRequested) ShowMessage("Api call cancelled");
                else ShowMessage("All done");
            }
        }

        public void OnClickStopApiButton(object sender, RoutedEventArgs e)
        {
            cts?.Cancel();
        }

        public void OnCheckedJapaneseHeader(object sender, RoutedEventArgs e)
        {
            var radio = (RadioButton)sender;

            if ((bool)radio.IsChecked)
            {
                ViewModel.IsHeaderJP = true;
                ShowMessage(@"'緯度' & '経度' will be appended to header");
            }
            else
            {
                ViewModel.IsHeaderJP = false;
                ShowMessage(@"'Latitude' & 'Longitude' will be appended to header");
            }

        }

        #region Helper Functions

        public void SetDefaultOutputFolder(string selectedFile)
        {
            var outputFolderString = $"{Path.GetDirectoryName(selectedFile)}\\CSV_Exported";
            try
            {
                var defaulOutputFolder = Directory.CreateDirectory(outputFolderString).FullName;
                //throw new UnauthorizedAccessException();
                ViewModel.OutputFolder = defaulOutputFolder;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void PrepareOutputFiles()
        {
            if (String.IsNullOrEmpty(ViewModel.OutputFolder))
            {
                try
                {
                    SetDefaultOutputFolder(ViewModel.SelectedFile);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            var fileNameOnly = Path.GetFileNameWithoutExtension(ViewModel.SelectedFile);
            ViewModel.OkOutputFileUri = $"{ViewModel.OutputFolder}\\{fileNameOnly}_ok.csv";
            ViewModel.ErrorOutputFileUri = $"{ViewModel.OutputFolder}\\{fileNameOnly}_error.csv";
        }

        public void ShowMessage(string statusMessage)
        {
            ViewModel.StatusMessage = statusMessage;
        }

        public void UpdateSuccessCount(string successCount)
        {
            ViewModel.SuccessCount = successCount;
        }

        public void UpdateErrorCount(string errorCount)
        {
            ViewModel.ErrorCount = errorCount;
        }

        public void ShowErrorMessage(string msg)
        {
            ShowMessage($"[ ERROR ]  {msg}");
        }

        public void ShowSuccess(string msg, int counter)
        {
            UpdateSuccessCount($"{counter}");
            ShowMessage(msg);
        }

        public void ShowError(string msg, int counter)
        {
            UpdateErrorCount($"{counter}");
            ShowErrorMessage(msg);
        }

        public void ResetUIMessages()
        {
            ShowMessage("");
            UpdateSuccessCount("0");
            UpdateErrorCount("0");
        }

        public void EnableCallApiButton()
        {
            ViewModel.IsEnabledCallApiButton = true;
        }

        public void DisableCallApiButton()
        {
            ViewModel.IsEnabledCallApiButton = false;
        }

        public void EnableStopApiButton()
        {
            ViewModel.IsEnabledStopApiButton = true;
        }

        public void DisableStopApiButton()
        {
            ViewModel.IsEnabledStopApiButton = false;
        }

        public void DisableAllButtons()
        {
            DisableCallApiButton();
            DisableStopApiButton();
        }

        public void ToggleButtonState()
        {
            if (ViewModel.IsEnabledCallApiButton) DisableCallApiButton();
            else EnableCallApiButton();

            if (ViewModel.IsEnabledStopApiButton) DisableStopApiButton();
            else EnableStopApiButton();
        }

        #endregion
    }
}
