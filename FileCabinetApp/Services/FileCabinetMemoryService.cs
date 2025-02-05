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
        /// <param name="fileStream">The file stream used for reading and writing records.</param>
        /// <param name="validator">The validator used to validate record data.</param>
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
                Gender = gender
            };

            this.validator.ValidateParameters(record);
            int newId = this.GetNextId();
            record.Id = newId;

            this.fileStream.Seek(0, SeekOrigin.End);
            using (var writer = new BinaryWriter(this.fileStream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write((short)0);
                writer.Write(record.Id);
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
            return newId;
        }

        /// <summary>
        /// Edits an existing record in the file storage.
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
                Gender = gender
            };

            this.validator.ValidateParameters(record);

            this.fileStream.Seek(0, SeekOrigin.Begin);
            using (var reader = new BinaryReader(this.fileStream, Encoding.UTF8, leaveOpen: true))
            {
                while (this.fileStream.Position < this.fileStream.Length)
                {
                    long position = this.fileStream.Position;
                    short status = reader.ReadInt16();
                    int currentId = reader.ReadInt32();

                    this.fileStream.Seek(RecordSize - 6, SeekOrigin.Current);

                    if (status == 0 && currentId == id)
                    {
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
        /// Gets all active records from the file storage.
        /// </summary>
        /// <returns>A read-only collection of active records.</returns>
        public ReadOnlyCollection<FileCabinetRecord> GetRecords()
        {
            var records = new List<FileCabinetRecord>();

            this.fileStream.Seek(0, SeekOrigin.Begin);
            using (var reader = new BinaryReader(this.fileStream, Encoding.UTF8, leaveOpen: true))
            {
                while (this.fileStream.Position < this.fileStream.Length)
                {
                    long startPos = this.fileStream.Position;
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

                    if (status == 0)
                    {
                        records.Add(new FileCabinetRecord
                        {
                            Id = id,
                            FirstName = firstName,
                            LastName = lastName,
                            DateOfBirth = new DateTime(year, month, day),
                            Height = height,
                            Salary = salary,
                            Gender = (char)genderValue
                        });
                    }
                }
            }

            return new ReadOnlyCollection<FileCabinetRecord>(records);
        }

        /// <summary>
        /// Returns the count of active records (status=0).
        /// </summary>
        /// <returns>The number of active records.</returns>
        public int GetStat()
        {
            int count = 0;
            this.fileStream.Seek(0, SeekOrigin.Begin);
            using (var reader = new BinaryReader(this.fileStream, Encoding.UTF8, leaveOpen: true))
            {
                while (this.fileStream.Position < this.fileStream.Length)
                {
                    short status = reader.ReadInt16();
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
        /// Finds records by first name.
        /// </summary>
        /// <param name="firstName">The first name to match (case-insensitive).</param>
        /// <returns>A read-only collection of matching records.</returns>
        public ReadOnlyCollection<FileCabinetRecord> FindByFirstName(string firstName)
        {
            var allRecords = this.GetRecords();
            var result = allRecords.Where(r =>
                r.FirstName.Equals(firstName, StringComparison.OrdinalIgnoreCase)).ToList();
            return new ReadOnlyCollection<FileCabinetRecord>(result);
        }

        /// <summary>
        /// Finds records by last name.
        /// </summary>
        /// <param name="lastName">The last name to match (case-insensitive).</param>
        /// <returns>A read-only collection of matching records.</returns>
        public ReadOnlyCollection<FileCabinetRecord> FindByLastName(string lastName)
        {
            var allRecords = this.GetRecords();
            var result = allRecords.Where(r =>
                r.LastName.Equals(lastName, StringComparison.OrdinalIgnoreCase)).ToList();
            return new ReadOnlyCollection<FileCabinetRecord>(result);
        }

        /// <summary>
        /// Finds records by date of birth.
        /// </summary>
        /// <param name="dateOfBirth">The date of birth to match.</param>
        /// <returns>A read-only collection of matching records.</returns>
        public ReadOnlyCollection<FileCabinetRecord> FindByDateOfBirth(DateTime dateOfBirth)
        {
            var allRecords = this.GetRecords();
            var result = allRecords.Where(r => r.DateOfBirth == dateOfBirth).ToList();
            return new ReadOnlyCollection<FileCabinetRecord>(result);
        }

        /// <summary>
        /// Creates a snapshot of current data.
        /// </summary>
        /// <returns>A FileCabinetServiceSnapshot with all active records.</returns>
        public FileCabinetServiceSnapshot MakeSnapshot()
        {
            var records = this.GetRecords();
            return new FileCabinetServiceSnapshot(records);
        }

        /// <summary>
        /// Merges or updates data from the given snapshot.
        /// Active records in the file are updated if the IDs match; new records are appended if they do not exist.
        /// Invalid or conflicting records are skipped.
        /// </summary>
        /// <param name="snapshot">The snapshot to restore from.</param>
        public void Restore(FileCabinetServiceSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var importedRecords = snapshot.Records;
            foreach (var record in importedRecords)
            {
                try
                {
                    this.validator.ValidateParameters(record);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Import error: record #{record.Id}, {ex.Message}. Skipped.");
                    continue;
                }

                // We can either update an existing record in place or add new at the end.
                // Let's do a simplified approach:
                var existing = this.FindRecordPosition(record.Id);
                if (existing >= 0)
                {
                    // Update in place
                    this.UpdateRecordInFile(record, existing);
                }
                else
                {
                    // Append as new
                    this.AppendRecordToFile(record);
                }
            }
        }

        private int GetNextId()
        {
            return this.GetStat() + 1;
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

        /// <summary>
        /// Searches the file for a record with the given ID.
        /// Returns the byte-position if found; otherwise returns -1.
        /// </summary>
        /// <param name="id">The record ID to search for.</param>
        /// <returns>The byte offset of the record start, or -1 if not found.</returns>
        private long FindRecordPosition(int id)
        {
            this.fileStream.Seek(0, SeekOrigin.Begin);
            using (var reader = new BinaryReader(this.fileStream, Encoding.UTF8, leaveOpen: true))
            {
                while (this.fileStream.Position < this.fileStream.Length)
                {
                    long position = this.fileStream.Position;
                    short status = reader.ReadInt16();
                    int currentId = reader.ReadInt32();

                    if (status == 0 && currentId == id)
                    {
                        return position; // the beginning of this record
                    }

                    // Move to the next record
                    this.fileStream.Seek(RecordSize - 6, SeekOrigin.Current);
                }
            }

            return -1;
        }

        private void UpdateRecordInFile(FileCabinetRecord record, long position)
        {
            this.fileStream.Seek(position, SeekOrigin.Begin);
            using (var writer = new BinaryWriter(this.fileStream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write((short)0);
                writer.Write(record.Id);
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
        }

        private void AppendRecordToFile(FileCabinetRecord record)
        {
            this.fileStream.Seek(0, SeekOrigin.End);
            using (var writer = new BinaryWriter(this.fileStream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write((short)0);
                writer.Write(record.Id);
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
        }
    }
}
