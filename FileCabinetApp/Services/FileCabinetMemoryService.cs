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
        private readonly List<FileCabinetRecord> list = new List<FileCabinetRecord>();
        private readonly IRecordValidator validator;

        private readonly Dictionary<string, List<FileCabinetRecord>> firstNameDictionary =
            new Dictionary<string, List<FileCabinetRecord>>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, List<FileCabinetRecord>> lastNameDictionary =
            new Dictionary<string, List<FileCabinetRecord>>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<DateTime, List<FileCabinetRecord>> dateOfBirthDictionary =
            new Dictionary<DateTime, List<FileCabinetRecord>>();

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
            var existing = this.list.Find(r => r.Id == id);
            if (existing == null)
            {
                throw new ArgumentException($"Record with ID {id} not found.");
            }

            this.RemoveFromDictionaries(existing);

            var temp = new FileCabinetRecord
            {
                Id = id,
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = dateOfBirth,
                Height = height,
                Salary = salary,
                Gender = gender,
            };

            this.validator.ValidateParameters(temp);

            existing.FirstName = temp.FirstName;
            existing.LastName = temp.LastName;
            existing.DateOfBirth = temp.DateOfBirth;
            existing.Height = temp.Height;
            existing.Salary = temp.Salary;
            existing.Gender = temp.Gender;

            this.UpdateDictionaries(existing);
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
        /// Returns the total count of records in memory (since none are "deleted" conceptually).
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
            if (this.firstNameDictionary.TryGetValue(firstName, out var records))
            {
                return new ReadOnlyCollection<FileCabinetRecord>(records);
            }

            return new ReadOnlyCollection<FileCabinetRecord>(new List<FileCabinetRecord>());
        }

        /// <summary>
        /// Finds records by last name.
        /// </summary>
        /// <param name="lastName">The last name.</param>
        /// <returns>A read-only collection of matching records.</returns>
        public ReadOnlyCollection<FileCabinetRecord> FindByLastName(string lastName)
        {
            if (this.lastNameDictionary.TryGetValue(lastName, out var records))
            {
                return new ReadOnlyCollection<FileCabinetRecord>(records);
            }

            return new ReadOnlyCollection<FileCabinetRecord>(new List<FileCabinetRecord>());
        }

        /// <summary>
        /// Finds records by date of birth.
        /// </summary>
        /// <param name="dateOfBirth">The date of birth.</param>
        /// <returns>A read-only collection of matching records.</returns>
        public ReadOnlyCollection<FileCabinetRecord> FindByDateOfBirth(DateTime dateOfBirth)
        {
            if (this.dateOfBirthDictionary.TryGetValue(dateOfBirth.Date, out var records))
            {
                return new ReadOnlyCollection<FileCabinetRecord>(records);
            }

            return new ReadOnlyCollection<FileCabinetRecord>(new List<FileCabinetRecord>());
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

        /// <summary>
        /// Removes a record by ID (physically from the list and dictionaries).
        /// </summary>
        /// <param name="id">The record ID to remove.</param>
        /// <returns>True if a record was removed, false if not found.</returns>
        public bool RemoveRecord(int id)
        {
            var record = this.list.Find(r => r.Id == id);
            if (record == null)
            {
                return false;
            }

            this.RemoveFromDictionaries(record);
            this.list.Remove(record);
            return true;
        }

        /// <summary>
        /// Returns the number of deleted records (always 0 for memory service).
        /// </summary>
        /// <returns>0, because memory-based removal physically deletes the record.</returns>
        public int GetDeletedCount()
        {
            return 0;
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

            DateTime key = record.DateOfBirth.Date;
            if (!this.dateOfBirthDictionary.TryGetValue(key, out var dobRecords))
            {
                dobRecords = new List<FileCabinetRecord>();
                this.dateOfBirthDictionary[key] = dobRecords;
            }
            dobRecords.Add(record);
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

            var key = record.DateOfBirth.Date;
            if (this.dateOfBirthDictionary.TryGetValue(key, out var dobRecords))
            {
                dobRecords.Remove(record);
            }
        }
    }
}