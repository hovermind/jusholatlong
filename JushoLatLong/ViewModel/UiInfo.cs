using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JushoLatLong.ViewModel
{

    class UiInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string SelectedFile { get; set; }
        public string OutputFolder { get; set; }
        public string MapApiKey { get; set; }

        public string StatusMessage { get; set; }
        public string SuccessCount { get; set; }
        public string ErrorCount { get; set; }

        public UiInfo()
        {
            SelectedFile = "";
            OutputFolder = "";
            MapApiKey = "";

            StatusMessage = "...";
            SuccessCount = "0";
            ErrorCount = "0";
        }
    }
}
