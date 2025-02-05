using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using FileCabinetApp.Models;

namespace FileCabinetApp.Readers
{
    /// <summary>
    /// Reads FileCabinetRecord objects from a CSV file.
    /// </summary>
    public class FileCabinetRecordCsvReader
    {
        private readonly StreamReader reader;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileCabinetRecordCsvReader"/> class.
        /// </summary>
        /// <param name="reader">The stream reader to read CSV lines from.</param>
        public FileCabinetRecordCsvReader(StreamReader reader)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        /// <summary>
        /// Reads all CSV lines and returns a list of FileCabinetRecord objects.
        /// </summary>
        /// <returns>A list of parsed records.</returns>
        public IList<FileCabinetRecord> ReadAll()
        {
            var records = new List<FileCabinetRecord>();

            // Optionally read the header line (if any)
            string? headerLine = this.reader.ReadLine();

            // Example header: "Id,FirstName,LastName,DateOfBirth,Height,Salary,Gender"

            while (!this.reader.EndOfStream)
            {
                string? line = this.reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] fields = line.Split(',');

                // Expecting 7 columns:
                //  0: Id
                //  1: FirstName
                //  2: LastName
                //  3: DateOfBirth (MM/dd/yyyy)
                //  4: Height
                //  5: Salary
                //  6: Gender (M/F/N)
                if (fields.Length < 7)
                {
                    Console.WriteLine($"CSV parse warning: not enough fields. Skipped line: {line}");
                    continue;
                }

                try
                {
                    var record = ParseRecord(fields);
                    records.Add(record);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"CSV parse error: {ex.Message}. Skipped line: {line}");
                }
            }

            return records;
        }

        private static FileCabinetRecord ParseRecord(string[] fields)
        {
            if (!int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
            {
                throw new ArgumentException($"Invalid Id value: {fields[0]}");
            }

            string firstName = fields[1];
            string lastName = fields[2];

            if (!DateTime.TryParse(
                    fields[3],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime dateOfBirth))
            {
                throw new ArgumentException($"Invalid DateOfBirth value: {fields[3]}");
            }

            if (!short.TryParse(fields[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out short height))
            {
                throw new ArgumentException($"Invalid Height value: {fields[4]}");
            }

            if (!decimal.TryParse(fields[5], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal salary))
            {
                throw new ArgumentException($"Invalid Salary value: {fields[5]}");
            }

            if (string.IsNullOrEmpty(fields[6]))
            {
                throw new ArgumentException("Gender is empty or null.");
            }

            char gender = fields[6][0];

            return new FileCabinetRecord
            {
                Id = id,
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = dateOfBirth,
                Height = height,
                Salary = salary,
                Gender = gender,
            };
        }
    }
}