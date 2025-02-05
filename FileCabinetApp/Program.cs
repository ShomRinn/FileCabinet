using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Xml;
using FileCabinetApp.Models;
using FileCabinetApp.Services;
using FileCabinetApp.Validators;

namespace FileCabinetApp
{
    /// <summary>
    /// The main entry point of the File Cabinet Application.
    /// </summary>
    public static class Program
    {
        private const string DeveloperName = "Heorhi Bachyla";
        private const string HintMessage = "Enter your command, or enter 'help' to get help.";
        private const string DefaultValidationMessage = "Using default validation rules.";
        private const string CustomValidationMessage = "Using custom validation rules.";

        private static string storageType = "file";
        private static IFileCabinetService? fileCabinetService;
        private static bool isRunning = true;

        private static readonly (string Command, Action<string> Action)[] Commands = new (string, Action<string>)[]
        {
            ("help", PrintHelp),
            ("exit", Exit),
            ("stat", Stat),
            ("create", Create),
            ("list", List),
            ("edit", Edit),
            ("find", Find),
            ("export", Export),
            ("import", Import),
            ("remove", Remove), // Step 9: remove command
            ("purge", Purge),   // Step 9: purge command
        };

        /// <summary>
        /// Application entry point.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        public static void Main(string[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args), "The argument 'args' cannot be null.");
            }

            SetStorageType(args);
            SetValidationRules(args);

            Console.WriteLine($"File Cabinet Application, developed by {DeveloperName}");
            Console.WriteLine(HintMessage);
            Console.WriteLine();

            do
            {
                Console.Write("> ");
                var line = Console.ReadLine();
                var inputs = line?.Split(' ', 2) ?? new string[] { string.Empty, string.Empty };
                var command = inputs[0];

                if (string.IsNullOrEmpty(command))
                {
                    Console.WriteLine(HintMessage);
                    continue;
                }

                var index = Array.FindIndex(Commands, c => c.Command.Equals(command, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                {
                    var parameters = inputs.Length > 1 ? inputs[1] : string.Empty;
                    Commands[index].Action(parameters);
                }
                else
                {
                    PrintMissedCommandInfo(command);
                }
            }
            while (isRunning);
        }

        /// <summary>
        /// Determines the storage type (memory/file) from command-line arguments.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        private static void SetStorageType(string[] args)
        {
            foreach (var arg in args)
            {
                if (arg.StartsWith("--storage=", StringComparison.OrdinalIgnoreCase))
                {
                    storageType = arg.Substring("--storage=".Length).ToLowerInvariant();
                }
                else if (arg.StartsWith("-s", StringComparison.OrdinalIgnoreCase))
                {
                    storageType = arg.Substring(2).ToLowerInvariant();
                }
            }
        }

        /// <summary>
        /// Determines the validation rules (default/custom) from command-line arguments,
        /// then initializes the IFileCabinetService depending on storageType.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        private static void SetValidationRules(string[] args)
        {
            string validationRules = "DEFAULT";

            foreach (var arg in args)
            {
                if (arg.StartsWith("--validation-rules=", StringComparison.OrdinalIgnoreCase))
                {
                    validationRules = arg.Substring("--validation-rules=".Length).ToUpperInvariant();
                }
                else if (arg.StartsWith("-v", StringComparison.OrdinalIgnoreCase))
                {
                    validationRules = arg.Substring(2).ToUpperInvariant();
                }
            }

            IRecordValidator validator = validationRules switch
            {
                "DEFAULT" => new DefaultValidator(),
                "CUSTOM" => new CustomValidator(),
                _ => new DefaultValidator(),
            };

            if (validationRules != "DEFAULT" && validationRules != "CUSTOM")
            {
                Console.WriteLine($"Unknown validation rules: {validationRules}. Using default rules.");
            }

            Console.WriteLine(validationRules == "CUSTOM" ? CustomValidationMessage : DefaultValidationMessage);

            switch (storageType)
            {
                case "file":
                    Console.WriteLine("Using file system storage.");
                    var fileStream = new FileStream("cabinet-records.db", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    fileCabinetService = new FileCabinetFilesystemService(fileStream, validator);
                    break;

                case "memory":
                default:
                    Console.WriteLine("Using memory storage.");
                    fileCabinetService = new FileCabinetMemoryService(validator);
                    break;
            }
        }

        /// <summary>
        /// Prints a message if the command does not exist.
        /// </summary>
        /// <param name="command">The unknown command.</param>
        private static void PrintMissedCommandInfo(string command)
        {
            Console.WriteLine($"There is no '{command}' command.");
            Console.WriteLine();
        }

        /// <summary>
        /// Prints the help information or detailed info about a specific command.
        /// </summary>
        /// <param name="parameters">The command name for details.</param>
        private static void PrintHelp(string parameters)
        {
            var helpMessages = new (string Command, string ShortDescription, string DetailedDescription)[]
            {
                ("help", "Displays a list of all available commands. Use 'help <command>' for details about a specific command.", "Displays a list of all available commands. Use 'help <command>' for details about a specific command."),
                ("exit", "Exits the application", "Terminates the program."),
                ("stat", "Shows the statistics of records", "Displays the total number of records (and how many are marked as deleted in file-based service)."),
                ("create", "Creates a new record with personal data", "Allows you to add a new record by entering personal details step by step."),
                ("list", "Lists all records in the system", "Displays all records stored in the system."),
                ("edit", "Modifies an existing record by ID", "Allows you to update the details of an existing record. Usage: edit <record ID>."),
                ("find", "Searches records by property", "Searches for records by a specific property (firstname, lastname, or dateofbirth). Usage: find <property> <value>."),
                ("export", "Exports records to a file.", "Exports all records to a specified file in CSV or XML format. Usage: export <csv/xml> <file path>."),
                ("import", "Imports records from a file.", "Imports records from CSV or XML. Usage: import <csv/xml> <file path>."),
                ("remove", "Removes a record by ID", "Usage: remove <record ID>. For file-based service, marks the record as deleted."),
                ("purge", "Defragments the data file (file-based only).", "Usage: purge. Moves all active records to remove 'empty' spaces from deleted records."),
            };

            if (!string.IsNullOrEmpty(parameters))
            {
                var commandDescription = Array.Find(helpMessages, h => h.Command.Equals(parameters, StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrEmpty(commandDescription.Command))
                {
                    Console.WriteLine($"There is no explanation for the '{parameters}' command.");
                }
                else
                {
                    Console.WriteLine($"{commandDescription.Command} - {commandDescription.ShortDescription}");
                    Console.WriteLine($"Details: {commandDescription.DetailedDescription}");
                }
            }
            else
            {
                Console.WriteLine("Available commands:");
                foreach (var (command, shortDescription, _) in helpMessages)
                {
                    Console.WriteLine($"\t{command} - {shortDescription}");
                }
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Exits the application.
        /// </summary>
        /// <param name="parameters">Not used.</param>
        private static void Exit(string parameters)
        {
            Console.WriteLine("Exiting the application...");
            isRunning = false;
        }

        /// <summary>
        /// Prints the number of records and how many are marked as deleted (for file-based service).
        /// </summary>
        /// <param name="parameters">Not used.</param>
        private static void Stat(string parameters)
        {
            if (fileCabinetService is null)
            {
                Console.WriteLine("File cabinet service not initialized.");
                return;
            }

            // We assume FileCabinetFilesystemService provides a method GetDeletedCount().
            // If memory-based, that method may always return 0, or not exist.
            // Adjust as needed if your code differs.
            int total = fileCabinetService.GetStat();
            int deleted = 0;

            if (fileCabinetService is FileCabinetFilesystemService fsService)
            {
                deleted = fsService.GetDeletedCount();
            }

            Console.WriteLine($"{total} record(s). {deleted} record(s) are deleted.");
        }

        /// <summary>
        /// Creates a new record with user input.
        /// </summary>
        /// <param name="parameters">Not used.</param>
        private static void Create(string parameters)
        {
            if (fileCabinetService is null)
            {
                Console.WriteLine("File cabinet service not initialized.");
                return;
            }

            string firstName, lastName;
            DateTime dateOfBirth;
            short height;
            decimal salary;
            char gender;

            while (true)
            {
                try
                {
                    Console.Write("First name: ");
                    firstName = Console.ReadLine() ?? string.Empty;

                    Console.Write("Last name: ");
                    lastName = Console.ReadLine() ?? string.Empty;

                    Console.Write("Date of birth (MM/DD/YYYY): ");
                    if (!DateTime.TryParse(Console.ReadLine(), CultureInfo.InvariantCulture, DateTimeStyles.None, out dateOfBirth))
                    {
                        Console.WriteLine("Invalid date format.");
                        continue;
                    }

                    Console.Write("Height (cm): ");
                    if (!short.TryParse(Console.ReadLine(), out height))
                    {
                        Console.WriteLine("Invalid height format.");
                        continue;
                    }

                    Console.Write("Salary: ");
                    if (!decimal.TryParse(Console.ReadLine(), out salary))
                    {
                        Console.WriteLine("Invalid salary format.");
                        continue;
                    }

                    Console.Write("Gender (M/F/N): ");
                    string genderInput = Console.ReadLine() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(genderInput) || genderInput.Length != 1)
                    {
                        Console.WriteLine("Invalid gender format.");
                        continue;
                    }

                    gender = char.ToUpper(genderInput[0], CultureInfo.InvariantCulture);
                    var recordId = fileCabinetService.CreateRecord(firstName, lastName, dateOfBirth, height, salary, gender);
                    Console.WriteLine($"Record #{recordId} is created.");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Lists all records in the current service.
        /// </summary>
        /// <param name="parameters">Not used.</param>
        private static void List(string parameters)
        {
            if (fileCabinetService is null)
            {
                Console.WriteLine("File cabinet service not initialized.");
                return;
            }

            var records = fileCabinetService.GetRecords();

            if (records == null || records.Count == 0)
            {
                Console.WriteLine("No records found.");
                return;
            }

            foreach (var record in records)
            {
                Console.WriteLine(
                    $"#{record.Id}, {record.FirstName}, {record.LastName}, " +
                    $"{record.DateOfBirth:yyyy-MMM-dd}, {record.Height} cm, {record.Salary:C}, {record.Gender}");
            }
        }

        /// <summary>
        /// Edits an existing record by ID.
        /// </summary>
        /// <param name="parameters">Should be the record ID.</param>
        private static void Edit(string parameters)
        {
            if (fileCabinetService is null)
            {
                Console.WriteLine("File cabinet service not initialized.");
                return;
            }

            if (!int.TryParse(parameters, out int id) || id <= 0)
            {
                Console.WriteLine("Invalid ID.");
                return;
            }

            try
            {
                Console.Write("First name: ");
                string firstName = Console.ReadLine() ?? string.Empty;

                Console.Write("Last name: ");
                string lastName = Console.ReadLine() ?? string.Empty;

                Console.Write("Date of birth (MM/DD/YYYY): ");
                if (!DateTime.TryParse(Console.ReadLine(), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateOfBirth))
                {
                    Console.WriteLine("Invalid date format.");
                    return;
                }

                Console.Write("Height (cm): ");
                if (!short.TryParse(Console.ReadLine(), out short height))
                {
                    Console.WriteLine("Invalid height format.");
                    return;
                }

                Console.Write("Salary: ");
                if (!decimal.TryParse(Console.ReadLine(), out decimal salary))
                {
                    Console.WriteLine("Invalid salary format.");
                    return;
                }

                Console.Write("Gender (M/F/N): ");
                string genderInput = Console.ReadLine() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(genderInput) || genderInput.Length != 1)
                {
                    Console.WriteLine("Invalid gender format.");
                    return;
                }

                char gender = char.ToUpper(genderInput[0], CultureInfo.InvariantCulture);
                fileCabinetService.EditRecord(id, firstName, lastName, dateOfBirth, height, salary, gender);
                Console.WriteLine($"Record #{id} is updated.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Finds records by a specified property (FIRSTNAME, LASTNAME, DATEOFBIRTH).
        /// </summary>
        /// <param name="parameters">Property and value separated by space.</param>
        private static void Find(string parameters)
        {
            if (fileCabinetService is null)
            {
                Console.WriteLine("File cabinet service not initialized.");
                return;
            }

            var inputs = parameters.Split(' ', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (inputs.Length != 2)
            {
                Console.WriteLine("Invalid command format. Use: find <property> <value>");
                return;
            }

            string property = inputs[0].ToUpperInvariant();
            string value = inputs[1].Trim('"').Trim();

            ReadOnlyCollection<FileCabinetRecord> results = property switch
            {
                "FIRSTNAME" => fileCabinetService.FindByFirstName(value),
                "LASTNAME" => fileCabinetService.FindByLastName(value),
                "DATEOFBIRTH" when DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dob)
                    => fileCabinetService.FindByDateOfBirth(dob),
                _ => new ReadOnlyCollection<FileCabinetRecord>(Array.Empty<FileCabinetRecord>()),
            };

            if (results.Count == 0)
            {
                Console.WriteLine("No records found.");
                return;
            }

            PrintRecords(results);
        }

        /// <summary>
        /// Helper method to print a collection of records.
        /// </summary>
        /// <param name="records">Records to print.</param>
        private static void PrintRecords(IReadOnlyCollection<FileCabinetRecord> records)
        {
            foreach (var record in records)
            {
                Console.WriteLine(
                    $"#{record.Id}, {record.FirstName}, {record.LastName}, " +
                    $"{record.DateOfBirth:yyyy-MMM-dd}, {record.Height} cm, {record.Salary:C}, {record.Gender}");
            }
        }

        /// <summary>
        /// Exports records to a specified file in CSV or XML format.
        /// </summary>
        /// <param name="parameters">Format and file name.</param>
        private static void Export(string parameters)
        {
            if (fileCabinetService is null)
            {
                Console.WriteLine("File cabinet service not initialized.");
                return;
            }

            var inputs = parameters.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (inputs.Length != 2)
            {
                Console.WriteLine("Invalid command format. Usage: export <csv|xml> <filename>");
                return;
            }

            var format = inputs[0].ToUpperInvariant();
            var fileName = inputs[1];

            if (string.IsNullOrEmpty(fileName))
            {
                Console.WriteLine("File name is missing.");
                return;
            }

            try
            {
                using var streamWriter = new StreamWriter(fileName, false);
                var snapshot = fileCabinetService.MakeSnapshot();

                switch (format)
                {
                    case "CSV":
                        snapshot.SaveToCsv(streamWriter);
                        Console.WriteLine($"All records are exported to file {fileName}.");
                        break;

                    case "XML":
                        using (var xmlWriter = XmlWriter.Create(streamWriter, new XmlWriterSettings { Indent = true }))
                        {
                            snapshot.SaveToXml(xmlWriter);
                        }
                        Console.WriteLine($"All records are exported to file {fileName}.");
                        break;

                    default:
                        Console.WriteLine($"Unsupported export format: {format}");
                        break;
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Export failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Imports records from a CSV or XML file.
        /// </summary>
        /// <param name="parameters">Format and file name.</param>
        private static void Import(string parameters)
        {
            if (fileCabinetService is null)
            {
                Console.WriteLine("File cabinet service not initialized.");
                return;
            }

            var inputs = parameters.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (inputs.Length != 2)
            {
                Console.WriteLine("Usage: import <csv|xml> <filename>");
                return;
            }

            string format = inputs[0].ToLowerInvariant();
            string fileName = inputs[1];

            if (!File.Exists(fileName))
            {
                Console.WriteLine($"Import error: file {fileName} is not exist.");
                return;
            }

            switch (format)
            {
                case "csv":
                    ImportCsv(fileName);
                    break;

                case "xml":
                    ImportXml(fileName);
                    break;

                default:
                    Console.WriteLine($"Unknown import format: {format}");
                    break;
            }
        }

        /// <summary>
        /// Imports records from a CSV file.
        /// </summary>
        /// <param name="fileName">The CSV file name.</param>
        private static void ImportCsv(string fileName)
        {
            try
            {
                using var reader = new StreamReader(fileName);
                var snapshot = new FileCabinetServiceSnapshot(new ReadOnlyCollection<FileCabinetRecord>(Array.Empty<FileCabinetRecord>()));
                snapshot.LoadFromCsv(reader);

                fileCabinetService.Restore(snapshot);

                int importedCount = snapshot.Records.Count;
                Console.WriteLine($"{importedCount} records were imported from {fileName}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Import CSV error: {ex.Message}");
            }
        }

        /// <summary>
        /// Imports records from an XML file.
        /// </summary>
        /// <param name="fileName">The XML file name.</param>
        private static void ImportXml(string fileName)
        {
            try
            {
                using var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                var snapshot = new FileCabinetServiceSnapshot(new ReadOnlyCollection<FileCabinetRecord>(Array.Empty<FileCabinetRecord>()));
                snapshot.LoadFromXml(fs);

                fileCabinetService.Restore(snapshot);

                int importedCount = snapshot.Records.Count;
                Console.WriteLine($"{importedCount} records were imported from {fileName}.");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Import XML error: {ex.Message}");
            }
            catch (System.InvalidOperationException ex)
            {
                Console.WriteLine($"Import XML error: {ex.Message}");
            }
        }

        /// <summary>
        /// Removes a record by ID. Step 9 - remove command.
        /// </summary>
        /// <param name="parameters">The ID of the record to remove.</param>
        private static void Remove(string parameters)
        {
            if (fileCabinetService is null)
            {
                Console.WriteLine("File cabinet service not initialized.");
                return;
            }

            if (!int.TryParse(parameters, out int id) || id <= 0)
            {
                Console.WriteLine("Invalid ID format.");
                return;
            }

            try
            {
                bool success = fileCabinetService.RemoveRecord(id);
                if (success)
                {
                    Console.WriteLine($"Record #{id} is removed.");
                }
                else
                {
                    Console.WriteLine($"Record #{id} doesn't exist.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing record #{id}: {ex.Message}");
            }
        }

        /// <summary>
        /// Purges the data file (file-based service only). Step 9 - purge command.
        /// </summary>
        /// <param name="parameters">Not used.</param>
        private static void Purge(string parameters)
        {
            if (fileCabinetService is null)
            {
                Console.WriteLine("File cabinet service not initialized.");
                return;
            }

            // We check for an actual FileCabinetFilesystemService instance.
            // If memory-based, the command is not applicable.
            if (fileCabinetService is FileCabinetFilesystemService fsService)
            {
                // Suppose you implemented Purge to return (int totalBefore, int purged).
                var (totalBefore, purged) = fsService.Purge();
                Console.WriteLine($"Data file processing is completed: {purged} of {totalBefore} records were purged.");
            }
            else
            {
                Console.WriteLine("Purge is not applicable for the current storage type.");
            }
        }
    }
}