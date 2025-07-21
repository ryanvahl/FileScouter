// This can be the ONLY file with top-level statements and no other file can have a Main method for an entry point

// import here, should be a few lines, if even
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using OfficeOpenXml;
using ExcelDataReader; // need for older xls versions, rather than other options available
using System.Data;
using FileScouter.Models;

// Change the way some of the process is handled, it should be different

using FileScouter;

try
{
    // Log loading config and assigning config values with Serilog

    // Log object after values loaded as well to ensure input is correct

    // 3 folders up because the FileScouter program exe will be the relative location, this folder location is 3 away from the config.xml
    ScouterConfig scouterConfig = new ScouterConfig()
    {
        Config = XElement.Load("..\\..\\..\\Data\\config.xml")
    };

    scouterConfig.Paths = scouterConfig.Config.Element("FilePaths");
    scouterConfig.StartFolder = scouterConfig?.Paths?.Element("StartFolder")?.Value;
    scouterConfig.EndFolder = scouterConfig?.Paths?.Element("EndFolder")?.Value;
    scouterConfig.LogFile = scouterConfig?.Paths?.Element("LogFile")?.Value;

    Scouter.ScoutingBegins(scouterConfig!);
}
catch (Exception ex)
{
    Console.WriteLine($"Loading config error: {ex.Message}");
}