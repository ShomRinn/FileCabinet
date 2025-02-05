using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using FileCabinetApp.Models;

namespace FileCabinetGenerator
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string? outputType = null;
            string? outputFile = null;
            int recordsAmount = 0;
            int startId = 0;

            ParseArguments(args, ref outputType, ref outputFile, ref recordsAmount, ref startId);

            if (string.IsNullOrEmpty(outputType) || string.IsNullOrEmpty(outputFile) || recordsAmount <= 0 || startId < 0)
            {
                Console.WriteLine("Invalid arguments. Example usage:");
                Console.WriteLine("  FileCabinetGenerator.exe --output-type=csv --output=d:\\data\\records.csv --records-amount=10 --start-id=1");
                return;
            }

            var records = GenerateRecords(recordsAmount, startId);

            switch (outputType.ToLowerInvariant())
            {
                case "csv":
                    ExportCsv(records, outputFile);
                    Console.WriteLine($"{records.Count()} records were written to {outputFile}.");
                    break;

                case "xml":
                    ExportXml(records, outputFile);
                    Console.WriteLine($"{records.Count()} records were written to {outputFile}.");
                    break;

                default:
                    Console.WriteLine($"Unknown output type: {outputType}.");
                    break;
            }
        }

        private static void ParseArguments(
            string[] args,
            ref string? outputType,
            ref string? outputFile,
            ref int recordsAmount,
            ref int startId)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("--output-type=", StringComparison.OrdinalIgnoreCase))
                {
                    outputType = args[i].Substring("--output-type=".Length);
                }
                else if (args[i].StartsWith("-t", StringComparison.OrdinalIgnoreCase))
                {
                    outputType = args[i].Substring(2);
                }
                else if (args[i].StartsWith("--output=", StringComparison.OrdinalIgnoreCase))
                {
                    outputFile = args[i].Substring("--output=".Length);
                }
                else if (args[i].StartsWith("-o", StringComparison.OrdinalIgnoreCase))
                {
                    outputFile = args[i].Substring(2);
                }
                else if (args[i].StartsWith("--records-amount=", StringComparison.OrdinalIgnoreCase))
                {
                    var value = args[i].Substring("--records-amount=".Length);
                    if (int.TryParse(value, out int parsedA))
                    {
                        recordsAmount = parsedA;
                    }
                }
                else if (args[i].StartsWith("-a", StringComparison.OrdinalIgnoreCase))
                {
                    var value = args[i].Substring(2);
                    if (int.TryParse(value, out int parsedA))
                    {
                        recordsAmount = parsedA;
                    }
                }
                else if (args[i].StartsWith("--start-id=", StringComparison.OrdinalIgnoreCase))
                {
                    var value = args[i].Substring("--start-id=".Length);
                    if (int.TryParse(value, out int parsedI))
                    {
                        startId = parsedI;
                    }
                }
                else if (args[i].StartsWith("-i", StringComparison.OrdinalIgnoreCase))
                {
                    var value = args[i].Substring(2);
                    if (int.TryParse(value, out int parsedI))
                    {
                        startId = parsedI;
                    }
                }
            }
        }

        private static FileCabinetRecord[] GenerateRecords(int amount, int startId)
        {
            var records = new FileCabinetRecord[amount];
            var rnd = new Random();
            int currentId = startId;

            for (int i = 0; i < amount; i++)
            {
                string firstName = GenerateRandomString(rnd, 2, 10);
                string lastName = GenerateRandomString(rnd, 2, 10);

                int year = rnd.Next(1950, DateTime.Now.Year + 1);
                int month = rnd.Next(1, 13);
                int day = rnd.Next(1, 28);
                var dateOfBirth = new DateTime(year, month, day);

                short height = (short)rnd.Next(1, 300);
                decimal salary = (decimal)((rnd.NextDouble() * 10000) + 1);
                char gender = "MFN"[rnd.Next(0, 3)];

                records[i] = new FileCabinetRecord
                {
                    Id = currentId++,
                    FirstName = firstName,
                    LastName = lastName,
                    DateOfBirth = dateOfBirth,
                    Height = height,
                    Salary = salary,
                    Gender = gender
                };
            }

            return records;
        }

        private static string GenerateRandomString(Random rnd, int minLength, int maxLength)
        {
            int length = rnd.Next(minLength, maxLength + 1);
            const string letters = "abcdefghijklmnopqrstuvwxyz";
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                sb.Append(letters[rnd.Next(letters.Length)]);
            }

            // Capitalize first letter, etc.
            return sb.ToString();
        }

        private static void ExportCsv(FileCabinetRecord[] records, string fileName)
        {
            using (var writer = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                writer.WriteLine("Id,FirstName,LastName,DateOfBirth,Height,Salary,Gender");
                foreach (var r in records)
                {
                    writer.WriteLine($"{r.Id},{r.FirstName},{r.LastName},{r.DateOfBirth:MM/dd/yyyy},{r.Height},{r.Salary},{r.Gender}");
                }
            }
        }

        private static void ExportXml(FileCabinetRecord[] records, string fileName)
        {
            var container = new FileCabinetRecordsContainer
            {
                Records = records
            };

            using (var fs = new FileStream(fileName, FileMode.Create))
            {
                var serializer = new XmlSerializer(typeof(FileCabinetRecordsContainer));
                serializer.Serialize(fs, container);
            }
        }
    }

    [XmlRoot("records")]
    public class FileCabinetRecordsContainer
    {
        [XmlElement("record")]
        public FileCabinetRecord[]? Records { get; set; }
    }
}
