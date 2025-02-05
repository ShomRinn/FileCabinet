using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FileCabinetApp.Models;
using FileCabinetApp.Validators;

namespace FileCabinetApp.Services
{
    /// <summary>
    /// A memory-based service for storing FileCabinet records.
    /// </summary>
    public class FileCabinetMemoryService : IFileCabinetService
    {
        private readonly List<FileCabinetRecord> list = new();
        private readonly IRecordValidator validator;

        private readonly Dictionary<string, List<FileCabinetRecord>> firstNameDictionary =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, List<FileCabinetRecord>> lastNameDictionary =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<DateTime, List<FileCabinetRecord>> dateOfBirthDictionary =
            new();

        /// <summary>
        /// Initializes a new instance of the <see cref="FileCabinetMemoryService"/> class.
        /// </summary>
        /// <param name="validator">The record validator.</param>
        public FileCabinetMemoryService(IRecordValidator validator)
        {
            this.validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        /// <summary>
        /// Creates a new record in memory.
        /// </summary>
        /// <param name="firstName">The first name.</param>
        /// <param name="lastName">The last name.</param>
        /// <param name="dateOfBirth">The date of birth.</param>
        /// <param name="height">The height.</param>
        /// <param name="salary">The salary.</param>
        /// <param name="gender">The gender.</param>
        /// <returns>The ID of the newly created record.</returns>
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
                Id = this.list.Count + 1,
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = dateOfBirth,
                Height = height,
                Salary = salary,
                Gender = gender,
            };

            this.validator.ValidateParameters(record);

            this.list.Add(record);
            this.UpdateDictionaries(record);

            return record.Id;
        }

        /// <summary>
        /// Edits an existing record.
        /// </summary>
        /// <param name="id">The record ID.</param>
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
            var record = this.list.Find(r => r.Id == id);
            if (record == null)
            {
                throw new ArgumentException($"Record with ID {id} not found.");
            }

            this.RemoveFromDictionaries(record);

            var tempRecord = new FileCabinetRecord
            {
                Id = id,
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = dateOfBirth,
                Height = height,
                Salary = salary,
                Gender = gender,
            };

            this.validator.ValidateParameters(tempRecord);

            record.FirstName = tempRecord.FirstName;
            record.LastName = tempRecord.LastName;
            record.DateOfBirth = tempRecord.DateOfBirth;
            record.Height = tempRecord.Height;
            record.Salary = tempRecord.Salary;
            record.Gender = tempRecord.Gender;

            this.UpdateDictionaries(record);
        }

        /// <summary>
        /// Returns a read-only collection of all records in memory.
        /// </summary>
        /// <returns>A read-only collection.</returns>
        public ReadOnlyCollection<FileCabinetRecord> GetRecords()
        {
            return this.list.AsReadOnly();
        }

        /// <summary>
        /// Returns the total count of records in memory.
        /// </summary>
        /// <returns>The count of records.</returns>
        public int GetStat()
        {
            return this.list.Count;
        }

        /// <summary>
        /// Finds records by first name.
        /// </summary>
        /// <param name="firstName">The first name.</param>
        /// <returns>A read-only collection of matching records.</returns>
        public ReadOnlyCollection<FileCabinetRecord> FindByFirstName(string firstName)
        {
            return this.firstNameDictionary.TryGetValue(firstName, out var records)
                ? new ReadOnlyCollection<FileCabinetRecord>(records)
                : new ReadOnlyCollection<FileCabinetRecord>(new List<FileCabinetRecord>());
        }

        /// <summary>
        /// Finds records by last name.
        /// </summary>
        /// <param name="lastName">The last name.</param>
        /// <returns>A read-only collection of matching records.</returns>
        public ReadOnlyCollection<FileCabinetRecord> FindByLastName(string lastName)
        {
            return this.lastNameDictionary.TryGetValue(lastName, out var records)
                ? new ReadOnlyCollection<FileCabinetRecord>(records)
                : new ReadOnlyCollection<FileCabinetRecord>(new List<FileCabinetRecord>());
        }

        /// <summary>
        /// Finds records by date of birth.
        /// </summary>
        /// <param name="dateOfBirth">The date of birth.</param>
        /// <returns>A read-only collection of matching records.</returns>
        public ReadOnlyCollection<FileCabinetRecord> FindByDateOfBirth(DateTime dateOfBirth)
        {
            return this.dateOfBirthDictionary.TryGetValue(dateOfBirth.Date, out var records)
                ? new ReadOnlyCollection<FileCabinetRecord>(records)
                : new ReadOnlyCollection<FileCabinetRecord>(new List<FileCabinetRecord>());
        }

        /// <summary>
        /// Creates a snapshot of the current data.
        /// </summary>
        /// <returns>A FileCabinetServiceSnapshot containing all records.</returns>
        public FileCabinetServiceSnapshot MakeSnapshot()
        {
            return new FileCabinetServiceSnapshot(this.list.AsReadOnly());
        }

        /// <summary>
        /// Restores data from the specified snapshot.
        /// Existing records with the same ID are updated; new ones are added.
        /// Invalid records are skipped with an error message.
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

                var existing = this.list.Find(r => r.Id == record.Id);
                if (existing != null)
                {
                    this.RemoveFromDictionaries(existing);

                    existing.FirstName = record.FirstName;
                    existing.LastName = record.LastName;
                    existing.DateOfBirth = record.DateOfBirth;
                    existing.Height = record.Height;
                    existing.Salary = record.Salary;
                    existing.Gender = record.Gender;

                    this.UpdateDictionaries(existing);
                }
                else
                {
                    this.list.Add(record);
                    this.UpdateDictionaries(record);
                }
            }
        }

        private void UpdateDictionaries(FileCabinetRecord record)
        {
            if (!this.firstNameDictionary.TryGetValue(record.FirstName, out var firstNameRecords))
            {
                firstNameRecords = new List<FileCabinetRecord>();
                this.firstNameDictionary[record.FirstName] = firstNameRecords;
            }

            firstNameRecords.Add(record);

            if (!this.lastNameDictionary.TryGetValue(record.LastName, out var lastNameRecords))
            {
                lastNameRecords = new List<FileCabinetRecord>();
                this.lastNameDictionary[record.LastName] = lastNameRecords;
            }

            lastNameRecords.Add(record);

            if (!this.dateOfBirthDictionary.TryGetValue(record.DateOfBirth.Date, out var dateOfBirthRecords))
            {
                dateOfBirthRecords = new List<FileCabinetRecord>();
                this.dateOfBirthDictionary[record.DateOfBirth.Date] = dateOfBirthRecords;
            }

            dateOfBirthRecords.Add(record);
        }

        private void RemoveFromDictionaries(FileCabinetRecord record)
        {
            if (this.firstNameDictionary.TryGetValue(record.FirstName, out var firstNameRecords))
            {
                firstNameRecords.Remove(record);
            }

            if (this.lastNameDictionary.TryGetValue(record.LastName, out var lastNameRecords))
            {
                lastNameRecords.Remove(record);
            }

            if (this.dateOfBirthDictionary.TryGetValue(record.DateOfBirth.Date, out var dateOfBirthRecords))
            {
                dateOfBirthRecords.Remove(record);
            }
        }
    }
}