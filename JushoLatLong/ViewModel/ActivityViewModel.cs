using PropertyChanged;
using System.ComponentModel;

namespace JushoLatLong.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    class ActivityViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string SelectedFile { get; set; } = "";
        public string OutputFolder { get; set; } = "";
        public string MapApiKey { get; set; } = "AIzaSyDEaVB3eTqPbjhVNYgyd2N8EAVDabuI_-k";

        public int AddressColumnIndex { get; set; } = 1;

        public bool IsEnabledCallApiButton { get; set; } = false;
        public bool IsEnabledStopApiButton { get; set; } = false;

        public string StatusMessage { get; set; } = "";
        public string SuccessCount { get; set; } = "0";
        public string ErrorCount { get; set; } = "0";
    }
}
