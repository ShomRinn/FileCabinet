using System;
using System.Globalization;

namespace FileCabinetApp
{
    public static class Program
    {
        private const string DeveloperName = "Heorhi Bachyla";
        private const string HintMessage = "Enter your command, or enter 'help' to get help.";
        private const int CommandHelpIndex = 0;
        private const int DescriptionHelpIndex = 1;
        private const int ExplanationHelpIndex = 2;

        private static readonly FileCabinetService fileCabinetService = new FileCabinetService();

        private static bool isRunning = true;

        private static Tuple<string, Action<string>>[] commands = new Tuple<string, Action<string>>[]
        {
            new Tuple<string, Action<string>>("help", PrintHelp),
            new Tuple<string, Action<string>>("exit", Exit),
            new Tuple<string, Action<string>>("stat", Stat),
            new Tuple<string, Action<string>>("create", Create),
            new Tuple<string, Action<string>>("list", List),
            new Tuple<string, Action<string>>("edit", Edit),
            new Tuple<string, Action<string>>("find", Find),
        };

        private static string[][] helpMessages = new string[][]
        {
            new string[] { "help", "prints the help screen", "The 'help' command prints the help screen." },
            new string[] { "exit", "exits the application", "The 'exit' command exits the application." },
            new string[] { "stat", "shows the statistics of records", "The 'stat' command shows the number of records." },
            new string[] { "create", "creates a new record", "The 'create' command creates a new record with personal data." },
            new string[] { "list", "lists all records", "The 'list' command lists all records in the system." },
            new string[] { "edit", "edits an existing record", "The 'edit' command modifies an existing record by ID." },
        };

        public static void Main(string[] args)
        {
            Console.WriteLine($"File Cabinet Application, developed by {Program.DeveloperName}");
            Console.WriteLine(Program.HintMessage);
            Console.WriteLine();

            do
            {
                Console.Write("> ");
                var line = Console.ReadLine();
                var inputs = line != null ? line.Split(' ', 2) : new string[] { string.Empty, string.Empty };
                const int commandIndex = 0;
                var command = inputs[commandIndex];

                if (string.IsNullOrEmpty(command))
                {
                    Console.WriteLine(Program.HintMessage);
                    continue;
                }

                var index = Array.FindIndex(commands, 0, commands.Length, i => i.Item1.Equals(command, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                {
                    const int parametersIndex = 1;
                    var parameters = inputs.Length > 1 ? inputs[parametersIndex] : string.Empty;
                    commands[index].Item2(parameters);
                }
                else
                {
                    PrintMissedCommandInfo(command);
                }
            }
            while (isRunning);
        }

        private static void PrintMissedCommandInfo(string command)
        {
            Console.WriteLine($"There is no '{command}' command.");
            Console.WriteLine();
        }

        private static void PrintHelp(string parameters)
        {
            if (!string.IsNullOrEmpty(parameters))
            {
                var index = Array.FindIndex(helpMessages, 0, helpMessages.Length, i => string.Equals(i[Program.CommandHelpIndex], parameters, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                {
                    Console.WriteLine(helpMessages[index][Program.ExplanationHelpIndex]);
                }
                else
                {
                    Console.WriteLine($"There is no explanation for '{parameters}' command.");
                }
            }
            else
            {
                Console.WriteLine("Available commands:");

                foreach (var helpMessage in helpMessages)
                {
                    Console.WriteLine("\t{0}\t- {1}", helpMessage[Program.CommandHelpIndex], helpMessage[Program.DescriptionHelpIndex]);
                }
            }

            Console.WriteLine();
        }

        private static void Exit(string parameters)
        {
            Console.WriteLine("Exiting an application...");
            isRunning = false;
        }

        private static void Stat(string parameters)
        {
            var recordsCount = fileCabinetService.GetStat();
            Console.WriteLine($"{recordsCount} record(s).");
        }

        private static void Create(string parameters)
        {
            string firstName = string.Empty, lastName = string.Empty;
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
                    if (string.IsNullOrWhiteSpace(firstName) || firstName.Length < 2 || firstName.Length > 60)
                    {
                        throw new ArgumentException("First name must be between 2 and 60 characters and cannot be empty or whitespace.");
                    }

                    Console.Write("Last name: ");
                    lastName = Console.ReadLine() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(lastName) || lastName.Length < 2 || lastName.Length > 60)
                    {
                        throw new ArgumentException("Last name must be between 2 and 60 characters and cannot be empty or whitespace.");
                    }

                    Console.Write("Date of birth (MM/DD/YYYY): ");
                    var dateInput = Console.ReadLine() ?? string.Empty;
                    dateOfBirth = DateTime.Parse(dateInput, CultureInfo.InvariantCulture);

                    Console.Write("Height (cm): ");
                    if (!short.TryParse(Console.ReadLine() ?? string.Empty, out height) || height <= 0 || height > 300)
                    {
                        throw new ArgumentException("Height must be a positive number and less than 300 cm.");
                    }

                    Console.Write("Salary: ");
                    if (!decimal.TryParse(Console.ReadLine() ?? string.Empty, out salary) || salary <= 0)
                    {
                        throw new ArgumentException("Salary must be a positive number.");
                    }

                    Console.Write("Gender (M/F/N): ");
                    var genderInput = Console.ReadLine() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(genderInput) || genderInput.Length != 1)
                    {
                        throw new ArgumentException("Gender must be a single character: 'M', 'F', or 'N'.");
                    }

                    gender = char.ToUpper(genderInput[0], CultureInfo.InvariantCulture);

                    var recordId = fileCabinetService.CreateRecord(firstName, lastName, dateOfBirth, height, salary, gender);
                    Console.WriteLine($"Record #{recordId} is created.");
                    break;
                }
                catch (FormatException ex)
                {
                    Console.WriteLine($"Input format error: {ex.Message}");
                }
                catch (OverflowException ex)
                {
                    Console.WriteLine($"Input value is too large or too small: {ex.Message}");
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"Validation error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error: {ex.Message}");
                }
            }
        }

        private static void List(string parameters)
        {
            var records = fileCabinetService.GetRecords();

            foreach (var record in records)
            {
                Console.WriteLine($"#{record.Id}, {record.FirstName}, {record.LastName}, {record.DateOfBirth:yyyy-MMM-dd}, {record.Height} cm, {record.Salary:C}, {record.Gender}");
            }
        }

        private static void Edit(string parameters)
        {
            if (string.IsNullOrWhiteSpace(parameters))
            {
                Console.WriteLine("You must specify the ID of the record to edit.");
                return;
            }

            if (!int.TryParse(parameters, out int id) || id <= 0)
            {
                Console.WriteLine("Invalid ID. Please enter a positive integer.");
                return;
            }

            try
            {
                Console.Write("First name: ");
                string firstName = Console.ReadLine() ?? string.Empty;

                Console.Write("Last name: ");
                string lastName = Console.ReadLine() ?? string.Empty;

                Console.Write("Date of birth (MM/DD/YYYY): ");
                if (!DateTime.TryParse(Console.ReadLine(), out DateTime dateOfBirth))
                {
                    throw new ArgumentException("Invalid date format.");
                }

                Console.Write("Height (cm): ");
                if (!short.TryParse(Console.ReadLine(), out short height) || height <= 0 || height > 300)
                {
                    throw new ArgumentException("Height must be a positive number and less than 300 cm.");
                }

                Console.Write("Salary: ");
                if (!decimal.TryParse(Console.ReadLine(), out decimal salary) || salary <= 0)
                {
                    throw new ArgumentException("Salary must be a positive number.");
                }

                Console.Write("Gender (M/F/N): ");
                string genderInput = Console.ReadLine() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(genderInput) || genderInput.Length != 1)
                {
                    throw new ArgumentException("Gender must be a single character: 'M', 'F', or 'N'.");
                }

                char gender = char.ToUpper(genderInput[0], CultureInfo.InvariantCulture);

                fileCabinetService.EditRecord(id, firstName, lastName, dateOfBirth, height, salary, gender);
                Console.WriteLine($"Record #{id} is updated.");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }
        }

        private static void Find(string parameters)
        {
            var inputs = parameters?.Split(' ', 2);

            if (inputs == null || inputs.Length < 2)
            {
                Console.WriteLine("Invalid parameters. Usage: find <property> <value>");
                return;
            }

            var property = inputs[0].ToLowerInvariant();
            var value = inputs[1].Trim('"');

            switch (property)
            {
                case "firstname":
                    PrintRecords(fileCabinetService.FindByFirstName(value));
                    break;
                default:
                    Console.WriteLine($"The property '{property}' is not supported for search.");
                    break;
            }
        }

        private static void PrintRecords(FileCabinetRecord[] records)
        {
            foreach (var record in records)
            {
                Console.WriteLine($"#{record.Id}, {record.FirstName}, {record.LastName}, {record.DateOfBirth:yyyy-MMM-dd}");
            }
        }
    }
}