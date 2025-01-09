using System;
using System.Collections.Generic;
using System.Linq;

namespace FileCabinetApp
{
    public class FileCabinetService
    {
        private readonly List<FileCabinetRecord> list = new List<FileCabinetRecord>();
        private readonly Dictionary<string, List<FileCabinetRecord>> firstNameDictionary = new (StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<FileCabinetRecord>> lastNameDictionary = new (StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<DateTime, List<FileCabinetRecord>> dateOfBirthDictionary = new ();

        public int CreateRecord(string firstName, string lastName, DateTime dateOfBirth, short height, decimal salary, char gender)
        {
            ValidateParameters(firstName, lastName, dateOfBirth, height, salary, gender);

            var record = new FileCabinetRecord
            {
                Id = this.list.Count + 1,
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = DateTime.SpecifyKind(dateOfBirth, DateTimeKind.Local),
                Height = height,
                Salary = salary,
                Gender = gender,
            };

            this.list.Add(record);
            this.UpdateDictionaries(record);
            return record.Id;
        }

        public FileCabinetRecord[] GetRecords()
        {
            return this.list.ToArray();
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

            ValidateParameters(firstName, lastName, dateOfBirth, height, salary, gender);

            record.FirstName = firstName;
            record.LastName = lastName;
            record.DateOfBirth = DateTime.SpecifyKind(dateOfBirth, DateTimeKind.Local);
            record.Height = height;
            record.Salary = salary;
            record.Gender = gender;

            this.UpdateDictionaries(record);
        }

        public FileCabinetRecord[] FindByFirstName(string firstName)
        {
            return this.firstNameDictionary.TryGetValue(firstName, out var records) ? records.ToArray() : Array.Empty<FileCabinetRecord>();
        }

        public FileCabinetRecord[] FindByLastName(string lastName)
        {
            return this.lastNameDictionary.TryGetValue(lastName, out var records) ? records.ToArray() : Array.Empty<FileCabinetRecord>();
        }

        public FileCabinetRecord[] FindByDateOfBirth(DateTime dateOfBirth)
        {
            return this.dateOfBirthDictionary.TryGetValue(dateOfBirth.Date, out var records) ? records.ToArray() : Array.Empty<FileCabinetRecord>();
        }

        private static void ValidateParameters(string firstName, string lastName, DateTime dateOfBirth, short height, decimal salary, char gender)
        {
            if (string.IsNullOrWhiteSpace(firstName) || firstName.Length < 2 || firstName.Length > 60)
            {
                throw new ArgumentException("First name must be between 2 and 60 characters and cannot be empty or contain only spaces.");
            }

            if (string.IsNullOrWhiteSpace(lastName) || lastName.Length < 2 || lastName.Length > 60)
            {
                throw new ArgumentException("Last name must be between 2 and 60 characters and cannot be empty or contain only spaces.");
            }

            var minDate = DateTime.SpecifyKind(new DateTime(1950, 1, 1), DateTimeKind.Utc);
            var currentDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
            if (dateOfBirth < minDate || dateOfBirth > currentDate)
            {
                throw new ArgumentException("Date of birth must be between 01-Jan-1950 and today's date.");
            }

            if (height <= 0 || height > 300)
            {
                throw new ArgumentException("Height must be a positive value and less than 300 cm.");
            }

            if (salary <= 0)
            {
                throw new ArgumentException("Salary must be a positive value.");
            }

            if (!"MFN".Contains(gender, StringComparison.Ordinal))
            {
                throw new ArgumentException("Gender must be 'M', 'F', or 'N'.");
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