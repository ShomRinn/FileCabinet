using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using FileCabinetApp.Models;
using FileCabinetApp.Validators;

namespace FileCabinetApp.Services
{
    /// <summary>
    /// Provides file-based storage for FileCabinet records.
    /// </summary>
    public class FileCabinetFilesystemService : IFileCabinetService
    {
        private const int RecordSize = 278;
        private readonly FileStream fileStream;
        private readonly IRecordValidator validator;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileCabinetFilesystemService"/> class.
        /// </summary>
        /// <param name="fileStream">The file stream.</param>
        /// <param name="validator">The record validator.</param>
        public FileCabinetFilesystemService(FileStream fileStream, IRecordValidator validator)
        {
            this.fileStream = fileStream ?? throw new ArgumentNullException(nameof(fileStream));
            this.validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        /// <summary>
        /// Creates a new record in the file storage.
        /// </summary>
        /// <param name="firstName">The first name.</param>
        /// <param name="lastName">The last name.</param>
        /// <param name="dateOfBirth">The date of birth.</param>
        /// <param name="height">The height.</param>
        /// <param name="salary">The salary.</param>
        /// <param name="gender">The gender.</param>
        /// <returns>The ID of the new record.</returns>
        public int CreateRecord(
            string firstName,
            string lastName,
            DateTime dateOfBirth,
            short height,
            decimal salary,
            char gender)
        {
            var record = new FileCabinetRecord
            {
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = dateOfBirth,
                Height = height,
                Salary = salary,
                Gender = gender,
            };

            this.validator.ValidateParameters(record);
            int newId = this.GetNextId();
            record.Id = newId;

            // Move the file pointer to the end so that new records are appended.
            this.fileStream.Seek(0, SeekOrigin.End);

            using (var writer = new BinaryWriter(this.fileStream, Encoding.UTF8, leaveOpen: true))
            {
                // Write the "active" status.
                writer.Write((short)0);

                // Write the ID.
                writer.Write(record.Id);

                // Write first name and last name as fixed-size strings.
                WriteFixedString(writer, record.FirstName, 60);
                WriteFixedString(writer, record.LastName, 60);

                // Write date of birth components (year, month, day).
                writer.Write(record.DateOfBirth.Year);
                writer.Write(record.DateOfBirth.Month);
                writer.Write(record.DateOfBirth.Day);

                // Write height, salary, and gender.
                writer.Write(record.Height);
                writer.Write(record.Salary);
                writer.Write((short)record.Gender);
            }

            // Ensure all data is flushed to disk.
            this.fileStream.Flush();
            return newId;
        }

        /// <summary>
        /// Returns all active records from the file.
        /// </summary>
        /// <returns>A read-only collection of active records.</returns>
        public ReadOnlyCollection<FileCabinetRecord> GetRecords()
        {
            var records = new List<FileCabinetRecord>();

            // Start reading from the beginning of the file.
            this.fileStream.Seek(0, SeekOrigin.Begin);

            using (var reader = new BinaryReader(this.fileStream, Encoding.UTF8, leaveOpen: true))
            {
                while (this.fileStream.Position < this.fileStream.Length)
                {
                    short status = reader.ReadInt16();
                    int id = reader.ReadInt32();
                    string firstName = ReadFixedString(reader, 60);
                    string lastName = ReadFixedString(reader, 60);
                    int year = reader.ReadInt32();
                    int month = reader.ReadInt32();
                    int day = reader.ReadInt32();
                    short height = reader.ReadInt16();
                    decimal salary = reader.ReadDecimal();
                    short genderValue = reader.ReadInt16();

                    // If status is 0, the record is considered active.
                    if (status == 0)
                    {
                        var record = new FileCabinetRecord
                        {
                            Id = id,
                            FirstName = firstName,
                            LastName = lastName,
                            DateOfBirth = new DateTime(year, month, day),
                            Height = height,
                            Salary = salary,
                            Gender = (char)genderValue,
                        };
                        records.Add(record);
                    }
                }
            }

            return new ReadOnlyCollection<FileCabinetRecord>(records);
        }

        /// <summary>
        /// Returns the number of active records in the file.
        /// </summary>
        /// <returns>The count of active records.</returns>
        public int GetStat()
        {
            int count = 0;

            // Start from the beginning to count active records.
            this.fileStream.Seek(0, SeekOrigin.Begin);

            using (var reader = new BinaryReader(this.fileStream, Encoding.UTF8, leaveOpen: true))
            {
                while (this.fileStream.Position < this.fileStream.Length)
                {
                    short status = reader.ReadInt16();

                    // Move to the next record (RecordSize - 2 bytes already read for status).
                    this.fileStream.Seek(RecordSize - 2, SeekOrigin.Current);

                    if (status == 0)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Updates an existing record in the file.
        /// </summary>
        /// <param name="id">The record ID to update.</param>
        /// <param name="firstName">The new first name.</param>
        /// <param name="lastName">The new last name.</param>
        /// <param name="dateOfBirth">The new date of birth.</param>
        /// <param name="height">The new height.</param>
        /// <param name="salary">The new salary.</param>
        /// <param name="gender">The new gender.</param>
        public void EditRecord(
            int id,
            string firstName,
            string lastName,
            DateTime dateOfBirth,
            short height,
            decimal salary,
            char gender)
        {
            var record = new FileCabinetRecord
            {
                Id = id,
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = dateOfBirth,
                Height = height,
                Salary = salary,
                Gender = gender,
            };

            this.validator.ValidateParameters(record);

            // Search from the beginning to find the record with the specified ID.
            this.fileStream.Seek(0, SeekOrigin.Begin);

            using (var reader = new BinaryReader(this.fileStream, Encoding.UTF8, leaveOpen: true))
            {
                while (this.fileStream.Position < this.fileStream.Length)
                {
                    long position = this.fileStream.Position;
                    short status = reader.ReadInt16();
                    int currentId = reader.ReadInt32();

                    // Skip the rest of this record, but keep track of where it ends.
                    this.fileStream.Seek(RecordSize - 6, SeekOrigin.Current);

                    // If we found an active record (status == 0) with the matching ID.
                    if (status == 0 && currentId == id)
                    {
                        // Move back to the beginning of this record.
                        this.fileStream.Seek(position, SeekOrigin.Begin);

                        using (var writer = new BinaryWriter(this.fileStream, Encoding.UTF8, leaveOpen: true))
                        {
                            writer.Write((short)0);
                            writer.Write(id);
                            WriteFixedString(writer, record.FirstName, 60);
                            WriteFixedString(writer, record.LastName, 60);
                            writer.Write(record.DateOfBirth.Year);
                            writer.Write(record.DateOfBirth.Month);
                            writer.Write(record.DateOfBirth.Day);
                            writer.Write(record.Height);
                            writer.Write(record.Salary);
                            writer.Write((short)record.Gender);
                        }

                        this.fileStream.Flush();
                        return;
                    }
                }
            }

            throw new ArgumentException($"Record with ID {id} not found.");
        }

        /// <summary>
        /// Finds records by first name.
        /// </summary>
        /// <param name="firstName">The first name to search for.</param>
        /// <returns>A read-only collection of matching records.</returns>
        public ReadOnlyCollection<FileCabinetRecord> FindByFirstName(string firstName)
        {
            var allRecords = this.GetRecords();
            var matchingRecords = allRecords
                .Where(r => r.FirstName.Equals(firstName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return new ReadOnlyCollection<FileCabinetRecord>(matchingRecords);
        }

        /// <summary>
        /// Finds records by last name.
        /// </summary>
        /// <param name="lastName">The last name to search for.</param>
        /// <returns>A read-only collection of matching records.</returns>
        public ReadOnlyCollection<FileCabinetRecord> FindByLastName(string lastName)
        {
            var allRecords = this.GetRecords();
            var matchingRecords = allRecords
                .Where(r => r.LastName.Equals(lastName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return new ReadOnlyCollection<FileCabinetRecord>(matchingRecords);
        }

        /// <summary>
        /// Finds records by date of birth.
        /// </summary>
        /// <param name="dateOfBirth">The date of birth to search for.</param>
        /// <returns>A read-only collection of matching records.</returns>
        public ReadOnlyCollection<FileCabinetRecord> FindByDateOfBirth(DateTime dateOfBirth)
        {
            var allRecords = this.GetRecords();
            var matchingRecords = allRecords
                .Where(r => r.DateOfBirth == dateOfBirth)
                .ToList();
            return new ReadOnlyCollection<FileCabinetRecord>(matchingRecords);
        }

        /// <summary>
        /// Creates a snapshot of current data.
        /// </summary>
        /// <returns>The service snapshot.</returns>
        public FileCabinetServiceSnapshot MakeSnapshot()
        {
            var allActiveRecords = this.GetRecords();
            return new FileCabinetServiceSnapshot(allActiveRecords);
        }

        private static void WriteFixedString(BinaryWriter writer, string value, int maxLength)
        {
            var subStr = value.Length > maxLength ? value[..maxLength] : value;
            for (int i = 0; i < maxLength; i++)
            {
                char c = i < subStr.Length ? subStr[i] : '\0';
                writer.Write(c);
            }
        }

        private static string ReadFixedString(BinaryReader reader, int length)
        {
            var chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = reader.ReadChar();
            }

            return new string(chars).TrimEnd('\0');
        }

        private int GetNextId()
        {
            // A simple approach to produce a new ID:
            // "Number of active records" + 1.
            // This may skip IDs of deleted records, but is enough for a basic case.
            return this.GetStat() + 1;
        }
    }
}