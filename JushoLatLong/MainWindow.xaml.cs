
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
using System.Linq;
using System.Windows.Controls;
using JushoLatLong.MapApi;
using JushoLatLong.Model.Api;

namespace JushoLatLong
{

    public partial class MainWindow : Window
    {
        // data context
        private ActivityViewModel viewModel = null;

        // api call cancellation token
        private CancellationTokenSource cts = null;

        // utils
        private IFileUtil fileUtil = null;
        private CommonUtil commonUtil = null;

        public MainWindow()
        {
            InitializeComponent();

            // set data context
            viewModel = new ActivityViewModel();
            DataContext = viewModel;

            fileUtil = new FileUtil();
            commonUtil = new CommonUtil();
        }

        public void OnClickBrowseFileButton(object sender, RoutedEventArgs e)
        {
            ResetUIMessages();

            // selected file (csv)
            var fileNameString = fileUtil?.GetSelectedFile("csv");
            viewModel.SelectedFile = fileNameString;

            if (!String.IsNullOrEmpty(fileNameString))
            {
                if (String.IsNullOrEmpty(viewModel.OutputFolder)) SetDefaultOutputFolder(fileNameString);
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

            viewModel.OutputFolder = outputFolderString;
        }

        public async void OnClickCallApiButton(object sender, RoutedEventArgs e)
        {
            ResetUIMessages();
            DisableCallApiButton();
            EnableStopApiButton();

            // validate api key & input file
            if (!(commonUtil.ValidateApiKey(this, viewModel) && fileUtil.ValidateInputFile(this, viewModel)))
            {
                EnableCallApiButton();
                DisableStopApiButton();

                return;
            }

            // prepare ouput files
            var validAddressCsvFile = fileUtil.PrepareFile("ok.csv", this, viewModel);
            var missingAdressCsvFIle = fileUtil.PrepareFile("error.csv", this, viewModel);
            if (!(validAddressCsvFile != "" && missingAdressCsvFIle != ""))
            {
                EnableCallApiButton();
                DisableStopApiButton();

                return;
            }

            // cancellation TokenSource
            cts?.Dispose();
            cts = new CancellationTokenSource();

            using (var csvReader = new CsvReader(new StreamReader(viewModel.SelectedFile, Encoding.UTF8)))
            using (var validCsvWriter = new CsvWriter(new StreamWriter(File.Open(validAddressCsvFile, FileMode.Truncate, FileAccess.ReadWrite), Encoding.UTF8)))
            using (var errorCsvWriter = new CsvWriter(new StreamWriter(File.Open(missingAdressCsvFIle, FileMode.Truncate, FileAccess.ReadWrite), Encoding.UTF8)))
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

                    var apiService = new GeocodingService(viewModel.MapApiKey);
                    GeoPoint mapPoint = null;

                    #region Writing Header

                    csvReader.Read();
                    var headers = csvReader.GetRecord<dynamic>();

                    // error csv file does not need extra column
                    errorCsvWriter.WriteRecord(headers);
                    errorCsvWriter.NextRecord();

                    // ok csv file needs 2 extra column
                    if (viewModel.IsHeaderJP)
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
                        if (cts.Token.IsCancellationRequested) break;

                        var profile = csvReader.GetRecord<dynamic>();                // entire row
                        var address = csvReader[viewModel.AddressColumnIndex - 1];   // address column

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
                        catch (WebException ex)
                        {
                            OnExceptionOccured(ex.Message);
                            return;
                        }
                    }
                });

                EnableCallApiButton();
                DisableStopApiButton();

                if (cts.Token.IsCancellationRequested)
                {
                    ShowMessage("Api call cancelled");
                }
                else
                {
                    ShowMessage("All done");
                }
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
                viewModel.IsHeaderJP = true;
                ShowMessage(@"'緯度' & '経度' will be appended to header");
            }
            else
            {
                viewModel.IsHeaderJP = false;
                ShowMessage(@"'Latitude' & 'Longitude' will be appended to header");
            }

        }

        #region Helper Functions

        public void SetDefaultOutputFolder(string selectedFile)
        {
            // early return
            if (String.IsNullOrEmpty(selectedFile))
            {
                viewModel.OutputFolder = "";
                return;
            }

            var outputFolderString = $"{Path.GetDirectoryName(selectedFile)}\\CSV_Exported";
            try
            {
                var defaulOutputFolder = Directory.CreateDirectory(outputFolderString).FullName;
                //throw new UnauthorizedAccessException();
                viewModel.OutputFolder = defaulOutputFolder;
            }
            catch (Exception ex)
            {
                MessageBox.Show(caption: "Output Folder Error",
                                 messageBoxText: $"Could not create defualt output folder \"{outputFolderString}\" [ Error: {ex.Message} ].\nPlease browse and select output folder before calling API.",
                                 button: MessageBoxButton.OK
                               );
            }
        }

        public void ShowMessage(string statusMessage)
        {
            viewModel.StatusMessage = statusMessage;
        }

        public void UpdateSuccessCount(string successCount)
        {
            viewModel.SuccessCount = successCount;
        }

        public void UpdateErrorCount(string errorCount)
        {
            viewModel.ErrorCount = errorCount;
        }

        public void ShowErrorMessage(string msg)
        {
            ShowMessage($"ERROR<{msg}>");
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

        public void OnExceptionOccured(string msg)
        {
            ShowMessage($"ERROR<{msg}>");

            EnableCallApiButton();
            DisableStopApiButton();
        }

        public void ResetUIMessages()
        {
            ShowMessage("");
            UpdateSuccessCount("0");
            UpdateErrorCount("0");
        }

        public void EnableCallApiButton()
        {
            viewModel.IsEnabledCallApiButton = true;
        }

        public void DisableCallApiButton()
        {
            viewModel.IsEnabledCallApiButton = false;
        }

        public void EnableStopApiButton()
        {
            viewModel.IsEnabledStopApiButton = true;
        }

        public void DisableStopApiButton()
        {
            viewModel.IsEnabledStopApiButton = false;
        }

        public void DisableAllButtons()
        {
            DisableCallApiButton();
            DisableStopApiButton();
        }

        #endregion
    }
}
