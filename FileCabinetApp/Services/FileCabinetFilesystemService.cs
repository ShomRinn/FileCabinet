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
        private const int RecordSize = 158; // total bytes per record
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
                Gender = gender,
            };

            this.validator.ValidateParameters(record);
            int newId = this.GetNextId();
            record.Id = newId;

            // status=0 => active (not deleted)
            this.fileStream.Seek(0, SeekOrigin.End);
            using (var writer = new BinaryWriter(this.fileStream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write((short)0); // status
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
            var updatedRecord = new FileCabinetRecord
            {
                Id = id,
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = dateOfBirth,
                Height = height,
                Salary = salary,
                Gender = gender
            };

            this.validator.ValidateParameters(updatedRecord);

            // Scan the file for an active record with matching ID
            this.fileStream.Seek(0, SeekOrigin.Begin);
            using (var reader = new BinaryReader(this.fileStream, Encoding.UTF8, leaveOpen: true))
            {
                while (this.fileStream.Position < this.fileStream.Length)
                {
                    long position = this.fileStream.Position;
                    short status = reader.ReadInt16();
                    int currentId = reader.ReadInt32();

                    // skip the rest of the record
                    this.fileStream.Seek(RecordSize - 6, SeekOrigin.Current);

                    // If status=0 (active) and IDs match, do an in-place update
                    if (!IsDeleted(status) && currentId == id)
                    {
                        this.UpdateRecordInFile(updatedRecord, position);
                        return;
                    }
                }
            }

            throw new ArgumentException($"Record with ID {id} not found or already deleted.");
        }

        /// <summary>
        /// Returns all active (not deleted) records from the file.
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

                    // Check if record is active
                    if (!IsDeleted(status))
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
        /// Returns the count of active (not deleted) records.
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

                    // skip rest of record
                    this.fileStream.Seek(RecordSize - 2, SeekOrigin.Current);

                    if (!IsDeleted(status))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Finds records by first name (case-insensitive).
        /// </summary>
        /// <param name="firstName">The first name to match.</param>
        /// <returns>A read-only collection of matching active records.</returns>
        public ReadOnlyCollection<FileCabinetRecord> FindByFirstName(string firstName)
        {
            var matches = this.GetRecords()
                .Where(r => r.FirstName.Equals(firstName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return new ReadOnlyCollection<FileCabinetRecord>(matches);
        }

        /// <summary>
        /// Finds records by last name (case-insensitive).
        /// </summary>
        /// <param name="lastName">The last name to match.</param>
        /// <returns>A read-only collection of matching active records.</returns>
        public ReadOnlyCollection<FileCabinetRecord> FindByLastName(string lastName)
        {
            var matches = this.GetRecords()
                .Where(r => r.LastName.Equals(lastName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return new ReadOnlyCollection<FileCabinetRecord>(matches);
        }

        /// <summary>
        /// Finds records by date of birth (exact match).
        /// </summary>
        /// <param name="dateOfBirth">The date of birth.</param>
        /// <returns>A read-only collection of matching active records.</returns>
        public ReadOnlyCollection<FileCabinetRecord> FindByDateOfBirth(DateTime dateOfBirth)
        {
            var matches = this.GetRecords()
                .Where(r => r.DateOfBirth == dateOfBirth)
                .ToList();

            return new ReadOnlyCollection<FileCabinetRecord>(matches);
        }

        /// <summary>
        /// Defragments the data file, removing any gaps from deleted records.
        /// Returns (totalBefore, purged).
        /// </summary>
        public (int totalBefore, int purged) Purge()
        {
            int totalBefore = 0;
            var activeRecords = new List<FileCabinetRecord>();
            int deletedCount = 0;

            // 1) Read everything
            this.fileStream.Seek(0, SeekOrigin.Begin);
            using (var reader = new BinaryReader(this.fileStream, Encoding.UTF8, leaveOpen: true))
            {
                while (this.fileStream.Position < this.fileStream.Length)
                {
                    totalBefore++;
                    long position = this.fileStream.Position;

                    short status = reader.ReadInt16();
                    int id = reader.ReadInt32();
                    string firstName = ReadFixedString(reader, 60);
                    string lastName = ReadFixedString(reader, 60);
                    int year = reader.ReadInt32();
                    int month = reader.ReadInt32();
                    int day = reader.ReadInt32();
                    short height = reader.ReadInt16();
                    decimal salary = reader.ReadDecimal();
                    short genderVal = reader.ReadInt16();

                    if (totalBefore != id)
                    {                        
                        activeRecords.Add(new FileCabinetRecord
                        {
                            Id = totalBefore,
                            FirstName = firstName,
                            LastName = lastName,
                            DateOfBirth = new DateTime(year, month, day),
                            Height = height,
                            Salary = salary,
                            Gender = (char)genderVal
                        });
                    }
                    else
                    {
                        deletedCount++;
                    }
                }
            }

            // 2) Wipe file
            //this.fileStream.SetLength(0);
            int positioner = ((totalBefore - 1) * 158) + 2;
            this.fileStream.Seek(positioner, SeekOrigin.Begin);

            // 3) Write back only active records
            using (var writer = new BinaryWriter(this.fileStream, Encoding.UTF8, leaveOpen: true))
            {
                    writer.Write(activeRecords.ElementAt(totalBefore).Id);
            }

            this.fileStream.Flush();

            // totalBefore includes both active + deleted
            // purged = number of deleted
            return (totalBefore, deletedCount);
        }

        /// <summary>
        /// Creates a snapshot of current data (active records).
        /// </summary>
        /// <returns>A FileCabinetServiceSnapshot with all active records.</returns>
        public FileCabinetServiceSnapshot MakeSnapshot()
        {
            var activeRecords = this.GetRecords();
            return new FileCabinetServiceSnapshot(activeRecords);
        }

        /// <summary>
        /// Restores data from the given snapshot.
        /// Updates or adds records as needed.
        /// </summary>
        /// <param name="snapshot">The snapshot to restore from.</param>
        public void Restore(FileCabinetServiceSnapshot snapshot)
        {
            if (snapshot is null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            foreach (var record in snapshot.Records)
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

                long pos = this.FindRecordPosition(record.Id);
                if (pos >= 0)
                {
                    this.UpdateRecordInFile(record, pos);
                }
                else
                {
                    this.AppendRecordToFile(record);
                }
            }
        }

        /// <summary>
        /// Marks a record as deleted.
        /// </summary>
        /// <param name="id">The record ID.</param>
        /// <returns>True if successfully marked as deleted, false if not found.</returns>
        public bool RemoveRecord(int id)
        {
            this.fileStream.Seek(0, SeekOrigin.Begin);

            using (var reader = new BinaryReader(this.fileStream, Encoding.UTF8, leaveOpen: true))
            {
                while (this.fileStream.Position < this.fileStream.Length)
                {
                    long position = this.fileStream.Position;
                    short status = reader.ReadInt16();
                    int currentId = reader.ReadInt32();

                    // skip rest
                    this.fileStream.Seek(RecordSize - 6, SeekOrigin.Current);

                    if (!IsDeleted(status) && currentId == id)
                    {
                        this.MarkRecordAsDeleted(position);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Counts how many records in the file are marked as deleted.
        /// </summary>
        /// <returns>The number of deleted records.</returns>
        public int GetDeletedCount()
        {
            int deletedCount = 0;

            this.fileStream.Seek(0, SeekOrigin.Begin);
            using (var reader = new BinaryReader(this.fileStream, Encoding.UTF8, leaveOpen: true))
            {
                while (this.fileStream.Position < this.fileStream.Length)
                {
                    short status = reader.ReadInt16();

                    // skip rest
                    this.fileStream.Seek(RecordSize - 2, SeekOrigin.Current);

                    if (IsDeleted(status))
                    {
                        deletedCount++;
                    }
                }
            }

            return deletedCount;
        }

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

                    if (!IsDeleted(status) && currentId == id)
                    {
                        return position;
                    }

                    this.fileStream.Seek(RecordSize - 6, SeekOrigin.Current);
                }
            }
            return -1;
        }

        private void MarkRecordAsDeleted(long position)
        {
            // Read the current status first (using BinaryReader)
            this.fileStream.Seek(position, SeekOrigin.Begin);
            short currentStatus;
            using (var reader = new BinaryReader(this.fileStream, Encoding.UTF8, leaveOpen: true))
            {
                currentStatus = reader.ReadInt16();
            }

            // Set the deleted bit
            short newStatus = SetIsDeletedBit(currentStatus, true);

            // Write it back
            this.fileStream.Seek(position, SeekOrigin.Begin);
            using (var writer = new BinaryWriter(this.fileStream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(newStatus);
            }

            this.fileStream.Flush();
        }

        private void UpdateRecordInFile(FileCabinetRecord record, long position)
        {
            // Read status first
            this.fileStream.Seek(position, SeekOrigin.Begin);
            short currentStatus;
            using (var reader = new BinaryReader(this.fileStream, Encoding.UTF8, leaveOpen: true))
            {
                currentStatus = reader.ReadInt16();
            }

            // Clear the deleted bit if we want to ensure the record is active
            short newStatus = SetIsDeletedBit(currentStatus, false);

            // Write the updated status + fields
            this.fileStream.Seek(position, SeekOrigin.Begin);
            using (var writer = new BinaryWriter(this.fileStream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(newStatus);
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
                short status = 0; // active record
                writer.Write(status);
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

        private int GetNextId()
        {
            // Simple approach: active count + 1
            return this.GetStat() + 1;
        }

        private static bool IsDeleted(short status)
        {
            // We assume bit #1 => deleted
            return (status & 0b10) == 0b10;
        }

        private static short SetIsDeletedBit(short status, bool deleted)
        {
            // bit mask for bit #1 = 0b10
            if (deleted)
            {
                return (short)(status | 0b10);
            }
            else
            {
                return (short)(status & ~0b10);
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

        private static void WriteFixedString(BinaryWriter writer, string value, int maxLength)
        {
            var subStr = value.Length > maxLength ? value[..maxLength] : value;
            for (int i = 0; i < maxLength; i++)
            {
                char c = i < subStr.Length ? subStr[i] : '\0';
                writer.Write(c);
            }
        }
    }
}