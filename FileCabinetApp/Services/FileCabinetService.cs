using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FileCabinetApp.Models;
using FileCabinetApp.Validators;

namespace FileCabinetApp.Services
{
    public class FileCabinetService
    {
        private readonly List<FileCabinetRecord> list = new ();
        private readonly IRecordValidator validator;
        private readonly Dictionary<string, List<FileCabinetRecord>> firstNameDictionary = new (StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<FileCabinetRecord>> lastNameDictionary = new (StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<DateTime, List<FileCabinetRecord>> dateOfBirthDictionary = new ();

        public FileCabinetService(IRecordValidator validator)
        {
            this.validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public int CreateRecord(string firstName, string lastName, DateTime dateOfBirth, short height, decimal salary, char gender)
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

        public ReadOnlyCollection<FileCabinetRecord> GetRecords()
        {
            return this.list.AsReadOnly();
        }

        public int GetStat()
        {
            return this.list.Count;
        }

        public void EditRecord(int id, string firstName, string lastName, DateTime dateOfBirth, short height, decimal salary, char gender)
        {
            var record = this.list.Find(r => r.Id == id);
            if (record == null)
            {
                throw new ArgumentException($"Record with ID {id} not found.");
            }

            this.RemoveFromDictionaries(record);

            var updatedRecord = new FileCabinetRecord
            {
                Id = id,
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = dateOfBirth,
                Height = height,
                Salary = salary,
                Gender = gender,
            };

            this.validator.ValidateParameters(updatedRecord);

            record.FirstName = updatedRecord.FirstName;
            record.LastName = updatedRecord.LastName;
            record.DateOfBirth = updatedRecord.DateOfBirth;
            record.Height = updatedRecord.Height;
            record.Salary = updatedRecord.Salary;
            record.Gender = updatedRecord.Gender;

            this.UpdateDictionaries(record);
        }

        public ReadOnlyCollection<FileCabinetRecord> FindByFirstName(string firstName)
        {
            return this.firstNameDictionary.TryGetValue(firstName, out var records)
                ? new ReadOnlyCollection<FileCabinetRecord>(records)
                : new ReadOnlyCollection<FileCabinetRecord>(new List<FileCabinetRecord>());
        }

        public ReadOnlyCollection<FileCabinetRecord> FindByLastName(string lastName)
        {
            return this.lastNameDictionary.TryGetValue(lastName, out var records)
                ? new ReadOnlyCollection<FileCabinetRecord>(records)
                : new ReadOnlyCollection<FileCabinetRecord>(new List<FileCabinetRecord>());
        }

        public ReadOnlyCollection<FileCabinetRecord> FindByDateOfBirth(DateTime dateOfBirth)
        {
            return this.dateOfBirthDictionary.TryGetValue(dateOfBirth.Date, out var records)
                ? new ReadOnlyCollection<FileCabinetRecord>(records)
                : new ReadOnlyCollection<FileCabinetRecord>(new List<FileCabinetRecord>());
        }

        public FileCabinetServiceSnapshot MakeSnapshot()
        {
            return new FileCabinetServiceSnapshot(new ReadOnlyCollection<FileCabinetRecord>(this.list));
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