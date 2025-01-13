using System;
using FileCabinetApp.Models;

namespace FileCabinetApp.Validators
{
    public class CustomValidator : IRecordValidator
    {
        public void ValidateParameters(FileCabinetRecord record)
        {
            if (string.IsNullOrWhiteSpace(record?.FirstName) || record.FirstName.Length < 3 || record.FirstName.Length > 50)
            {
                throw new ArgumentException("First name must be between 3 and 50 characters.");
            }

            if (string.IsNullOrWhiteSpace(record.LastName) || record.LastName.Length < 3 || record.LastName.Length > 50)
            {
                throw new ArgumentException("Last name must be between 3 and 50 characters.");
            }

            DateTime minDate = new DateTime(1960, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            if (record.DateOfBirth < minDate || record.DateOfBirth > DateTime.Now.AddYears(-18))
            {
                throw new ArgumentException("Date of birth must be between 01-Jan-1960 and at least 18 years ago.");
            }

            if (record.Height < 50 || record.Height > 250)
            {
                throw new ArgumentException("Height must be between 50 and 250 cm.");
            }

            if (record.Salary < 1000)
            {
                throw new ArgumentException("Salary must be at least 1000.");
            }

            if (!"MFN".Contains(record.Gender, StringComparison.Ordinal))
            {
                throw new ArgumentException("Gender must be 'M', 'F', or 'N'.");
            }
        }
    }
}