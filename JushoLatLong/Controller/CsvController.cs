
using CsvHelper;
using JushoLatLong.Model.Domain;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JushoLatLong.Controller {

    class CsvController : ICsvController {

        //public IEnumerable<T> GetProfileListFromCsv<T>(string csvFile) {

        //    using (CsvReader csv = new CsvReader(new StreamReader(csvFile, Encoding.Default))) {

        //        csv.Configuration.Delimiter = ",";              // using "," instead of ";"
        //        csv.Configuration.HasHeaderRecord = false;      // can not map Japanese character to english property name

        //        //// header might be bad format (Japanese)
        //        //csv.Configuration.HeaderValidated = (isValid, headerNames, headerNameIndex, context) => {
        //        //    if (!isValid) {
        //        //        Debug.WriteLine($"Header matching ['{string.Join("', '", headerNames)}'] names at index {headerNameIndex} was not found.");
        //        //    }
        //        //};

        //        // some field can be missing
        //        csv.Configuration.MissingFieldFound = (headerNames, index, context) => {
        //            Debug.WriteLine($"Field with names ['{string.Join("', '", headerNames)}'] at index '{index}' was not found.");
        //        };

        //        var records = csv.GetRecords<T>();
        //        //Debug.WriteLine();

        //        return records;
        //    }
        //}
    }
}
