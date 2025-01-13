using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Xml;
using FileCabinetApp.Models;
using FileCabinetApp.Services;
using FileCabinetApp.Validators;

namespace FileCabinetApp
{
    public static class Program
    {
        private const string DeveloperName = "Heorhi Bachyla";
        private const string HintMessage = "Enter your command, or enter 'help' to get help.";
        private const string DefaultValidationMessage = "Using default validation rules.";
        private const string CustomValidationMessage = "Using custom validation rules.";

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
        };

        private static FileCabinetService? fileCabinetService;

        private static bool isRunning = true;

        public static void Main(string[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args), "The argument 'args' cannot be null.");
            }

            SetValidationRules(args);

            Console.WriteLine($"File Cabinet Application, developed by {DeveloperName}");
            Console.WriteLine(HintMessage);
            Console.WriteLine();

            do
            {
                Console.Write("> ");
                var line = Console.ReadLine();
                var inputs = line?.Split(' ', 2) ?? new string[] { string.Empty, string.Empty };
                const int commandIndex = 0;
                var command = inputs[commandIndex];

                if (string.IsNullOrEmpty(command))
                {
                    Console.WriteLine(HintMessage);
                    continue;
                }

                var index = Array.FindIndex(Commands, c => c.Command.Equals(command, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                {
                    const int parametersIndex = 1;
                    var parameters = inputs.Length > 1 ? inputs[parametersIndex] : string.Empty;
                    Commands[index].Action(parameters);
                }
                else
                {
                    PrintMissedCommandInfo(command);
                }
            }
            while (isRunning);
        }

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

            switch (validationRules)
            {
                case "DEFAULT":
                    Console.WriteLine(DefaultValidationMessage);
                    fileCabinetService = new FileCabinetService(new DefaultValidator());
                    break;

                case "CUSTOM":
                    Console.WriteLine(CustomValidationMessage);
                    fileCabinetService = new FileCabinetService(new CustomValidator());
                    break;

                default:
                    Console.WriteLine($"Unknown validation rules: {validationRules}. Using default rules.");
                    fileCabinetService = new FileCabinetService(new DefaultValidator());
                    break;
            }
        }

        private static void PrintMissedCommandInfo(string command)
        {
            Console.WriteLine($"There is no '{command}' command.");
            Console.WriteLine();
        }

        private static void PrintHelp(string parameters)
        {
            var helpMessages = new (string Command, string ShortDescription, string DetailedDescription)[]
            {
                ("help", "Displays a list of all available commands. Use 'help <command>' for details about a specific command.", "Displays a list of all available commands. Use 'help <command>' for details about a specific command."),
                ("exit", "Exits the application", "Terminates the program."),
                ("stat", "Shows the statistics of records", "Displays the total number of records currently stored in the system."),
                ("create", "Creates a new record with personal data", "Allows you to add a new record by entering personal details step by step."),
                ("list", "Lists all records in the system", "Displays all records stored in the system."),
                ("edit", "Modifies an existing record by ID", "Allows you to update the details of an existing record. Usage: edit <record ID>."),
                ("find", "Searches records by property", "Searches for records by a specific property (firstname, lastname, or dateofbirth). Usage: find <property> <value>."),
                ("export", "Exports records to a file.", "Exports all records to a specified file in CSV or XML format. Usage: export <csv/xml> <file path>."),
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

        private static void Exit(string parameters)
        {
            Console.WriteLine("Exiting the application...");
            isRunning = false;
        }

        private static void Stat(string parameters)
        {
            var count = fileCabinetService?.GetStat() ?? 0;
            Console.WriteLine($"{count} record(s).");
        }

        private static void Create(string parameters)
        {
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
                    var recordId = fileCabinetService?.CreateRecord(firstName, lastName, dateOfBirth, height, salary, gender);
                    Console.WriteLine($"Record #{recordId} is created.");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private static void List(string parameters)
        {
            var records = fileCabinetService?.GetRecords();

            if (records == null || records.Count == 0)
            {
                Console.WriteLine("No records found.");
                return;
            }

            foreach (var record in records)
            {
                Console.WriteLine($"#{record.Id}, {record.FirstName}, {record.LastName}, {record.DateOfBirth:yyyy-MMM-dd}, {record.Height} cm, {record.Salary:C}, {record.Gender}");
            }
        }

        private static void Edit(string parameters)
        {
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
                fileCabinetService?.EditRecord(id, firstName, lastName, dateOfBirth, height, salary, gender);
                Console.WriteLine($"Record #{id} is updated.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static void Find(string parameters)
        {
            var inputs = parameters.Split(' ', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (inputs.Length != 2)
            {
                Console.WriteLine("Invalid command format. Use: find <property> <value>");
                return;
            }

            string property = inputs[0].ToUpperInvariant();
            string value = inputs[1].Trim('"').Trim();

            ReadOnlyCollection<FileCabinetRecord> results = property.ToUpperInvariant() switch
            {
                "FIRSTNAME" => fileCabinetService?.FindByFirstName(value) ?? new List<FileCabinetRecord>().AsReadOnly(),
                "LASTNAME" => fileCabinetService?.FindByLastName(value) ?? new List<FileCabinetRecord>().AsReadOnly(),
                "DATEOFBIRTH" => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateOfBirth)
                    ? fileCabinetService?.FindByDateOfBirth(dateOfBirth) ?? new List<FileCabinetRecord>().AsReadOnly()
                    : new List<FileCabinetRecord>().AsReadOnly(),
                _ => new List<FileCabinetRecord>().AsReadOnly()
            };

            if (results.Count == 0)
            {
                Console.WriteLine("No records found.");
                return;
            }

            PrintRecords(results);
        }

        private static void PrintRecords(IReadOnlyCollection<FileCabinetRecord> records)
        {
            foreach (var record in records)
            {
                Console.WriteLine($"#{record.Id}, {record.FirstName}, {record.LastName}, {record.DateOfBirth:yyyy-MMM-dd}, {record.Height} cm, {record.Salary:C}, {record.Gender}");
            }
        }

        private static void Export(string parameters)
        {
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
                var snapshot = fileCabinetService?.MakeSnapshot();

                switch (format)
                {
                    case "CSV":
                        {
                            snapshot?.SaveToCsv(streamWriter);
                            Console.WriteLine($"All records are exported to file {fileName}.");
                            break;
                        }

                    case "XML":
                        {
                            using var xmlWriter = XmlWriter.Create(streamWriter, new XmlWriterSettings { Indent = true });
                            snapshot?.SaveToXml(xmlWriter);
                            Console.WriteLine($"All records are exported to file {fileName}.");
                            break;
                        }

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
    }
}