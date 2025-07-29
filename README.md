# File Scouter

## Description
- Finds files in the start folder, processes them with a procedure in the database.
- A scouter is one that scouts. To scout is to make a search for someone or something, something in this case being a file.
- The FileSystemWatcher provides the core of the functionality. Once events are enabled, the program is "scouting" (watching) for files. There are libraries to process the files and numerous checks for issues along the way.

## Config File Setup
- **File element**: Defines the configuration values for a specific file being scouted in the start folder.
- **StringOrSubstringToFind**: Determines what files will use the configuration values within the file element. The text value here will be matched against the files in the start folder, not including the file extension. To match a substring in the file name, the value in this element must have a file name in the start folder with hyphens (-) around the same value. For example, file name in start folder is ""find-me-here"", this element needs to have "me" as the value to find this file just by searching for "me".
- **StoredProcedure**: The processing procedure used for the file matched. The stored procedure's exact name here must exist in the PostgreSQL database.
- **Parameter**: Each parameter element corresponds to a column in the file. The expected data type for the column must be one of the data types being checked for in the StoredProcedureCaller function. The order of these parameter elements must match the order of the procedure's expected params.
- **FilePaths**: Location of folders and files relative to the FileScouter.exe location.

## Running and Use
1. Open with Visual Studio.
2. Use run button.
3. Use or create a file configuration in the config.xml.
4. Move a file to the start folder, a file will only be processed if it is created in the start folder. It cannot be in the start folder before running the code or edited to cause it to process. This file name or the substring between hyphens (if used) in the file name needs to match the value of the StringOrSubstringToFind element in the config.xml.
5. The file will be processed by the procedure it belongs to and is move to the end folder.
6. Verify the table(s) being inserted into have the file's data. These tables would be in the procedure mapping to the file name.

## Log
- The location is in the data folder.
- Information can be about what actions are happening, some results of actions, and errors.

## Testing
- A good test is to use 5 files. 1 CSV with a one word name, 1 CSV with a name that has the other CSV file name as a substring of this files name. Repeat this for Xlsx file type as well. Then 1 file that has either file type and uses one of their names as a substring of its name, but put hyphens around the substring.
- This test will check processing of both file types, demonstrates the same file type does not process incorrectly even if it contains the entire name of another file, demonstrates substrings within the file names don't match if they are not surrounded hyphens, and then shows a substring matching only the file with hyphens.
- Example. 1 CSV "customer", 1 CSV "customer_abc", 1 Xlsx "products", 1 Xlsx "products_food", 1 CSV "find-customer-here"

## Supported
- **Database**: PostgreSQL.
- **File Type**: CSV and Xlsx.
- **File Events**: Created.

## Future Work
- Can be extended to support processing with different database technology or doing different actions with the file data if the "procedure" is just a function.
- Organize files into logical folders such as utilities.
- Validation of file contents. This would be a bit beyond the scope of this project. There a basic checks for type.
- Extend how many date formats can be accepted.
- Processing a file into the database is done per row in the file, 5 rows, 5 database requests. This is not great for performance (network calls and/or interaction outside your program is slower, this would need to use OS now, database engine, etc.) or integrity. How would you know which rows succeeded? The file has the same amount of data, nothing removed to indicate it processed into the database or failed to, and the inserted records cannot currently be mapped to the files to see what made it into the database. Ideally, find a way to bulk process all rows as one transaction, all rows or no rows. Maybe the database technology for PostgreSQL can process bulk/batch inserts if you basically build the inserts into a list and pass as a list.
- Processing CSV and Xlsx files appear to have some redundancy that may be able to have utility/helper functions for shared code.
- Make this run through files without needing to wait for keypress to exit. This will allow it to be ran by task scheduler. Right now, this is ran on demand to process files, then needs to be closed. This is likely most useful as a scheduled task, to make this possible, the code needs to do something that allows it to process files until the queue (folder with files to process) is empty or some other solution. Probably an IO library function to check contents of folder.
- Does this need to loop all the files after storing created file names in a queue or will it process the files fast enough? Can't have readykey code in it.

## Fixes
- If start and end folders do not exist in FileScouter project level, then create them and add .gitkeep to keep them tracked in Git, but this is in case they somehow get removed.
- If a file has an issue being processed, and the issue is corrected, the file will not be processed unless it is removed from the start folder and added again (create event only).
