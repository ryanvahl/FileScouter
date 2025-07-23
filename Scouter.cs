using ExcelDataReader; // need for older xls versions, rather than other options available
using FileScouter.Models;
using Microsoft.Extensions.FileSystemGlobbing;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FileScouter
{
    internal class Scouter
    {
        private static FileSystemWatcher? fileScouter;        

        public static void ScoutingBegins(ScouterConfig scouterBeginsConfig)
        {
            // start logging
            LoggingUtil.ConfigureLoggingUtil();
            LoggingUtil.LogInfo(scouterBeginsConfig.StartFolder);
            if (scouterBeginsConfig == null || string.IsNullOrEmpty(scouterBeginsConfig.StartFolder) || string.IsNullOrEmpty(scouterBeginsConfig.EndFolder))
            {
                // Log one of these paths are null or has not been configured
                //Console.WriteLine("Null scouter config at start");
                LoggingUtil.LogError("Null scouter config at start");
                return;
            }

            // line needed per the package instructions
            ExcelPackage.License.SetNonCommercialOrganization("Scouter");

            // provides support for older files types, xls, this needs to be provided before any code uses an older file type
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            // Log file scouting started via FileSystemWatcher

            // starts listening for file system change notifications and raises events for directory and file changes
            // the argument, startFolder, is directory listened to for file events
            fileScouter = new FileSystemWatcher(scouterBeginsConfig.StartFolder);

            fileScouter.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size;
            fileScouter.Filter = "*.*";
            // starts the monitoring of file events
            fileScouter.EnableRaisingEvents = true;

            // s for sender, e for event, use a user-defined method to handle the Create event from FileSystemWatcher
            // is this line to call ScouterOnCreated once needed if we are going to loop the startFolder?
            // Seems to be needed from documentation, unsure, but this is not as the document shows, document shows just a function to handle
            // the event as argument, where this assigns the function to handle event with lambda expression (anon function)
            fileScouter.Created += (s, e) => ScouterOnCreated(e, scouterBeginsConfig);

            LoggingUtil.LogInfo("Press any key to stop scouting");
            Console.ReadKey();

            // ends the monitoring of file events
            fileScouter.EnableRaisingEvents = false;
            LoggingUtil.LogInfo("Files no longer being scouted. Logging closed and flushed.");
            LoggingUtil.LogClose();
        }

        private static void ScouterOnCreated(FileSystemEventArgs e, ScouterConfig scouterOnCreatedConfigs)
        {
            // Log a file is detected with Serilog, use event arg variable e to to e.Name
            LoggingUtil.LogInfo($"Detected file created {e.Name}");

            try
            {
                string fileNameNoExt = Path.GetFileNameWithoutExtension(e.FullPath).ToLower();

                var processforFile = scouterOnCreatedConfigs?.Config?.Descendants("File");

                if (processforFile == null)
                {
                    LoggingUtil.LogError("File element empty");
                    return;
                }

                string fileElSearchText = "";
                // determine which text to use for compare against config file element blocks
                if (fileNameNoExt.Contains('-'))
                {
                    // find text between hyphen
                    Match match = Regex.Match(fileNameNoExt, @"-(.*?)-");
                    fileElSearchText = match.Groups[1].Value;
                }
                else
                {
                    fileElSearchText = fileNameNoExt;
                }

                // goal is get element containing a matching file name and file extention, this will allow the correct stored proc to be used for this file 
                // when debugging, this portion will loop until a match is found
                var fileProcessEl = processforFile.FirstOrDefault(x =>
                {
                    // get value between element with file name or substring to match
                    string? stringOrSubstringToFind = x?.Element("StringOrSubstringToFind")?.ToString();
                    string? fileNameInXml = x?.Element("StringOrSubstringToFind")?.Value;                                                      

                    return fileElSearchText == fileNameInXml;
                });

                // checks if there is a File element selected
                if (fileProcessEl == null)
                {
                    LoggingUtil.LogError("No matching process found");
                    return;
                }

                string? storedProc = fileProcessEl?.Element("StoredProcedure")?.Value;
                LoggingUtil.LogInfo("Use stored proc: " + storedProc);
                bool isProcessSuccess = false;
                string extension = Path.GetExtension(e.FullPath).ToLowerInvariant();

                switch (extension)
                {
                    case ".csv":
                        isProcessSuccess = ProcessCsv(e.FullPath, storedProc, fileProcessEl);
                        LoggingUtil.LogInfo("Use CSV process");
                        break;
                    case ".xls":
                    case ".xlsx":
                        isProcessSuccess = ProcessExcel(e.FullPath, extension, storedProc, fileProcessEl);
                        LoggingUtil.LogInfo("Use Excel process");
                        break;
                    default:
                        LoggingUtil.LogError("File type not supported");
                        return;
                }

                if (isProcessSuccess)
                {
                    string fileForEndFolder = Path.Combine(scouterOnCreatedConfigs.EndFolder, e.Name);

                    if (File.Exists(fileForEndFolder))
                    {
                        LoggingUtil.LogInfo("File exists. Attempting to rename with date before moving file.");

                        // This section would try to delete the file with try/catch and error for not being able to delete
                        // Maybe make it rename the file to move (not the existing file), need a count or something

                        // this return will have to be in catch when using try/catch if error occurs
                        return;
                    }

                    try
                    {
                        LoggingUtil.LogInfo($"Attempting to move file to {fileForEndFolder}");
                        File.Move(e.FullPath, fileForEndFolder);
                    }
                    catch (IOException ex)
                    {
                        LoggingUtil.LogError($"Error moving file: {ex.Message}, file was NOT moved to {fileForEndFolder}");
                        return;
                    }
                }
                else
                {
                    LoggingUtil.LogError($"Stored procedure failure, file was not moved.");
                }
            }
            catch (Exception ex)
            {
                LoggingUtil.LogError($"Issue processing file somewhere after created {ex.Message}");
                return;
            }            
        }
        private static bool ProcessCsv(string filePath, string storedProcEl, XElement fileProcessElForCsv)
        {
            try
            {
                var parametersEl = fileProcessElForCsv?.Element("Parameters");

                if (parametersEl == null)
                {
                    LoggingUtil.LogError("Parameters element empty");
                    return false;
                }
                
                // the number of objects created for this list will equal the number of parameter elements nested in the parameters element of the config.xml
                List<Parameters> parameters = new List<Parameters>();
                foreach (XElement param in parametersEl.Elements("Parameter"))
                {
                    Parameters csvStoreProcVals = new Parameters();
                    csvStoreProcVals.Param = param.Element("Name")?.Value;
                    csvStoreProcVals.Type = param.Element("Type")?.Value;

                    parameters.Add(csvStoreProcVals);
                }

                if (parameters == null)
                {
                    LoggingUtil.LogError("No parameters found for stored procedure");
                    return false;
                }

                // stores a string array where each index in the array stores a string, this string has each column value for the row separated by a comma
                var fileLines = File.ReadAllLines(filePath);

                if (fileLines.Length <= 1)
                {
                    LoggingUtil.LogError("CSV does not contain data.");
                    return false;
                }
                
                // the parameters objects will keep having the value overwritten, this is fine as each loop of row data is processed before it is overwritten
                for (int i = 1; i < fileLines.Length; i++)
                {
                    // string array of each col value in a specific row
                    var cols = fileLines[i].Split(',');

                    // if row data does not equal the number of parameters for procedure then this will not work, order is crucial
                    if (cols.Length != parameters.Count)
                    {
                        LoggingUtil.LogError("Number of columns with data in rows do not match number of parameter elements in parameters element of config");
                        return false;
                    }

                    // assign each row value from csv file to the Value property of object
                    for (int j = 0; j < cols.Length; j++)
                    {
                        parameters[j].Value = cols[j];
                    }

                    // called PER row in file
                    bool isSuccess = StoredProcedureCaller.ProcessDataToDb(storedProcEl, filePath, parameters);

                    if (!isSuccess)
                    {
                        return false;
                    }
                }
                    return true;
            }
            catch (Exception ex)
            {
                LoggingUtil.LogError($"CSV processing error {ex.Message}");
                return false;
            }            
        }

        public static bool ProcessExcel(string filePath, string extension, string storedProc, XElement fileProcessElForExcel)
        {
            try
            {
                var parametersEl = fileProcessElForExcel?.Element("Parameters");

                if (parametersEl == null)
                {
                    LoggingUtil.LogError("Parameters element empty");
                    return false;
                }

                // the number of objects created for this list will equal the number of parameter elements nested in the parameters element of the config.xml
                List<Parameters> parameters = new List<Parameters>();
                foreach (XElement param in parametersEl.Elements("Parameter"))
                {
                    Parameters excelStoreProcVals = new Parameters();
                    excelStoreProcVals.Param = param.Element("Name")?.Value;
                    excelStoreProcVals.Type = param.Element("Type")?.Value;

                    parameters.Add(excelStoreProcVals);
                }

                if (parameters == null)
                {
                    LoggingUtil.LogError("No parameters found for stored procedure");
                    return false;
                }


                if (extension == ".xlsx")
                {
                    // create an excel workbook
                    using var package = new ExcelPackage(new FileInfo(filePath));
                    // get the first worksheet
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                    {
                        //Console.WriteLine("No worksheet found in Excel file");
                        LoggingUtil.LogError("No worksheet found in Excel file");
                        return false;
                    }

                    // if row data does not equal the number of parameters for procedure then this will not work, order is crucial
                    // object PER column of data for each row
                    if (worksheet.Dimension.Columns != parameters.Count)
                    {
                        LoggingUtil.LogError("Number of columns with data in rows do not match number of parameter elements in parameters element of config");
                        return false;
                    }
                    else
                    {
                        // rows and columns are 1-based, row 2 because row 1 is header. Dimension is the used range of worksheet, contains data. The Dimension.Rows is the number of the last row that contains data.
                        // so, this for is row by row of Excel
                        for (int row = 2; row <= worksheet.Dimension.Rows; row++)
                        {
                            // Dimension here is the same for rows but for column, what columns contain data. While .Columns is the last column that contains data
                            // now, the row is traversed left to right by column
                            var rowData = new string[worksheet.Dimension.Columns];
                            for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                            {
                                var dataFromCell = worksheet.Cells[row, col].Text;
                                // have to -1 because the ExcelPackage is 1-based but arrays (as we know) are not
                                rowData[col - 1] = dataFromCell;
                                parameters[col - 1].Value = dataFromCell;
                            }

                            // provide row data as inserted data to procedure
                            bool isSuccess = StoredProcedureCaller.ProcessDataToDb(storedProc, filePath, parameters);
                            if (!isSuccess)
                            {
                                return false;
                            }
                        }
                    }

                }
                else
                {
                    LoggingUtil.LogError("Extension not xlsx");
                    return false;
                }
                    return true;
            }
            catch (Exception ex)
            {
                LoggingUtil.LogError($"Error processing Excel file: {ex.Message}");
                return false;
            }            
        }
    }
}
