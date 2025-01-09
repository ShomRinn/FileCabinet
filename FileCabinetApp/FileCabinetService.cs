using System;
using System.Collections.Generic;

namespace FileCabinetApp
{
    public class FileCabinetService
    {
        private readonly List<FileCabinetRecord> list = new List<FileCabinetRecord>();

        public int CreateRecord(string firstName, string lastName, DateTime dateOfBirth, short height, decimal salary, char gender)
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

            this.list.Add(record);
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

            record.FirstName = firstName;
            record.LastName = lastName;
            record.DateOfBirth = dateOfBirth;
            record.Height = height;
            record.Salary = salary;
            record.Gender = gender;
        }

        public FileCabinetRecord[] FindByFirstName(string firstName)
        {
            return this.list
                .Where(record => string.Equals(record.FirstName, firstName, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }
    }
}