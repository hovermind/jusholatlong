using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JushoLatLong.Utils
{
    interface IFileUtil
    {
        string GetSelectedFile(string fileType);
        string GetOutputFolder();
        bool IsFileLocked(string fileName);
    }
}
