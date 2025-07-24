using FileScouter;
using FileScouter.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Globalization;
using System.Xml.Linq;

namespace FileScouter
{
    // should this be a static class and have the assigned value to config be 
    static class StoredProcedureCaller
    {
        // This needs to be pulled from app.config and 
        // Need to figure out what to do for PostgreSQL
        private static readonly string _connString = "";

        static StoredProcedureCaller()
        {   
            // Program is the class in your project and is used to find the user secrets element in your .csproj
            var builder = new ConfigurationBuilder().AddUserSecrets<Program>();

            var config = builder.Build();

            _connString = config["FileScouterConn"];
        }

        public static void TestProcessCsvTest()
        {
            try
            {
                using var conn = new NpgsqlConnection(_connString);
                conn.Open();

                // using keyword here will automatically close and dispose connection to db
                using var cmd = new NpgsqlCommand("CALL usp_insert_customers_csv(@first_name_usp, @last_name_usp, @age_usp)", conn);
                cmd.Parameters.AddWithValue("first_name_usp", "test2");
                cmd.Parameters.AddWithValue("last_name_usp", "person2");
                cmd.Parameters.AddWithValue("age_usp", 200);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);
                LoggingUtil.LogError(ex.Message);
            }
            // Log completion
        }

        public static bool ProcessDataToDb(string storedProc, string filePath, List<Parameters> dbParameters)
        {
            var result = 0;

            try
            {
                string procedureCall = $"CALL {storedProc}(";
                // REMEMBER this is ONE row of data from the file. Each object is each parameter for stored procedure
                // need to create stored procedure now
                for (int i = 0; i < dbParameters.Count; i++)
                {
                    // do something different for the last param
                    if (i < dbParameters.Count - 1)
                    {
                        procedureCall += dbParameters[i].Param + ",";
                    }
                    else
                    {
                        // cannot have comma after last param
                        procedureCall += dbParameters[i].Param;
                    }
                }
                // close procedure
                procedureCall += ")";

                using var conn = new NpgsqlConnection(_connString);
                conn.Open();

                using var cmd = new NpgsqlCommand(procedureCall, conn);

                // reptitive, consider refactor later
                foreach(var param in dbParameters)
                {
                    try
                    {
                        if (param.Type.ToLower() == "integer")
                        {
                            cmd.Parameters.AddWithValue(param.Param, int.Parse(param.Value));
                        }
                        else if (param.Type.ToLower() == "boolean")
                        {
                            cmd.Parameters.AddWithValue(param.Param, bool.Parse(param.Value));
                        }
                        else if (param.Type.ToLower() == "varchar")
                        {
                            cmd.Parameters.AddWithValue(param.Param, param.Value);
                        }
                        else if (param.Type.ToLower() == "date")
                        {
                            string[] formats = {"MM/dd/yy", "MM/dd/yyyy"};
                            DateTime parsedfileDate;
                            try
                            {
                                // this can ONLY support files with the format here for date, but you can create a string array and provide a collection of different formats in strings, then provide that array where the format string is currently for ParseExact
                                DateTime parsedFileDate = DateTime.ParseExact(param.Value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None);
                                // the NpgsqlDbType.Date ignores the time portion and automatically maps DateTime to Date in PostgreSQL
                                cmd.Parameters.AddWithValue(param.Param, NpgsqlDbType.Date, parsedFileDate);
                            }
                            catch (Exception ex)
                            {
                                LoggingUtil.LogError(ex.Message);
                                return false;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Column data type not supported. Use integer, boolean or text. Please check column values.");
                        }
                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine(ex.Message);
                        LoggingUtil.LogError(ex.Message);
                        return false;
                    }                    
                }

                result = cmd.ExecuteNonQuery();

            }
            catch (IndexOutOfRangeException ex)
            {
                //Console.WriteLine(ex.Message);
                LoggingUtil.LogError(ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);
                LoggingUtil.LogError(ex.Message);
                return false;
            }

            return true;
        }

        //public static bool ProcessCsvToDb(string storedProc, string filePath, XElement fileProcessElforCsv, string[] cols, int rowNum)
        //{
        //    // This needs to do what works above but dynamically building it. It is sort of dynamic in the sense you don't have to keep editing the code, just the config
        //    // can't know what the file will look like until shown it anyway

        //    //string commandForProc = $"CALL {storedProc}(";
        //    //foreach(var col in cols)
        //    //{
        //    //    commandForProc += $"@{col},";
        //    //}
        //    //// get rid of last comma, since last param in procedure
        //    //commandForProc = commandForProc.Trim(',');
        //    //commandForProc += ")";

        //    // create param for command in procedure, list will be used twice
        //    List<string> paramsForCmd = new List<string>();
        //    int i = 1;
        //    foreach (var col in cols)
        //    {
        //        string param = "@param" + i.ToString();
        //        paramsForCmd.Add(param);
        //        i++;
        //    }

        //    string commandForProc = $"CALL {storedProc}(";
        //    foreach (var item in paramsForCmd)
        //    {
        //        commandForProc += item + ",";
        //    }
        //    // get rid of last comma, since last param in procedure
        //    commandForProc = commandForProc.Trim(',');
        //    commandForProc += ")";

        //    try
        //    {
        //        using var conn = new NpgsqlConnection(connString);
        //        conn.Open();
        //                    // get rid of last comma, since last param in procedure
        //        using var cmd = new NpgsqlCommand(commandForProc, conn);

        //        foreach (var item in paramsForCmd)
        //        {
        //            cmd.Parameters.AddWithValue(item, );
        //        }
        //        // add 1, 2, 3, etc to the param to give a diff param name to each value
        //        //int i = 1;
        //        //foreach (var col in cols)
        //        //{
        //        //    string param = "@param" + i.ToString();
        //        //    cmd.Parameters.AddWithValue(param, col);
        //        //    i++;
        //        //}

        //        //cmd.ExecuteNonQuery();
        //    }
        //    catch(Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //    }

        //    return false;
        //}

    }
}
