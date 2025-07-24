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
        private static readonly string _connString = "";

        static StoredProcedureCaller()
        {   
            // Program is the class in your project and is used to find the user secrets element in your .csproj
            var builder = new ConfigurationBuilder().AddUserSecrets<Program>();

            var config = builder.Build();

            _connString = config["FileScouterConn"];
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
                        LoggingUtil.LogError(ex.Message);
                        return false;
                    }                    
                }

                result = cmd.ExecuteNonQuery();

            }
            catch (IndexOutOfRangeException ex)
            {
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
    }
}
