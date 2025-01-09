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

        private static bool isRunning = true;

        private static readonly FileCabinetService fileCabinetService = new FileCabinetService();

        private static Tuple<string, Action<string>>[] commands = new Tuple<string, Action<string>>[]
        {
            new Tuple<string, Action<string>>("help", PrintHelp),
            new Tuple<string, Action<string>>("exit", Exit),
            new Tuple<string, Action<string>>("stat", Stat),
            new Tuple<string, Action<string>>("create", Create),
            new Tuple<string, Action<string>>("list", List)
        };

        private static string[][] helpMessages = new string[][]
        {
            new string[] { "help", "prints the help screen", "The 'help' command prints the help screen." },
            new string[] { "exit", "exits the application", "The 'exit' command exits the application." },
            new string[] { "stat", "shows the statistics of records", "The 'stat' command shows the number of records." },
            new string[] { "create", "creates a new record", "The 'create' command creates a new record with personal data." },
            new string[] { "list", "lists all records", "The 'list' command lists all records in the system." }
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
            Console.Write("First name: ");
            var firstName = Console.ReadLine();

            Console.Write("Last name: ");
            var lastName = Console.ReadLine();

            Console.Write("Date of birth (MM/DD/YYYY): ");
            var dateOfBirthInput = Console.ReadLine();
            if (!DateTime.TryParse(dateOfBirthInput, new CultureInfo("en-US"), DateTimeStyles.None, out var dateOfBirth))
            {
                Console.WriteLine("Invalid date format.");
                return;
            }

            Console.Write("Height (cm): ");
            if (!short.TryParse(Console.ReadLine(), out var height))
            {
                Console.WriteLine("Invalid height format.");
                return;
            }

            Console.Write("Salary: ");
            if (!decimal.TryParse(Console.ReadLine(), out var salary))
            {
                Console.WriteLine("Invalid salary format.");
                return;
            }

            Console.Write("Gender (M/F): ");
            var genderInput = Console.ReadLine();
            if (string.IsNullOrEmpty(genderInput) || genderInput.Length != 1)
            {
                Console.WriteLine("Invalid gender format.");
                return;
            }

            var gender = genderInput[0];

            var recordId = fileCabinetService.CreateRecord(firstName, lastName, dateOfBirth, height, salary, gender);
            Console.WriteLine($"Record #{recordId} is created.");
        }

        private static void List(string parameters)
        {
            var records = fileCabinetService.GetRecords();

            foreach (var record in records)
            {
                Console.WriteLine($"#{record.Id}, {record.FirstName}, {record.LastName}, {record.DateOfBirth:yyyy-MMM-dd}, {record.Height} cm, {record.Salary:C}, {record.Gender}");
            }
        }
    }
}