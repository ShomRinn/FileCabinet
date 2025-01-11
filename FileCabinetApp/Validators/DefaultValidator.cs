using System;
using FileCabinetApp.Models;

namespace FileCabinetApp.Validators
{
    public class DefaultValidator : IRecordValidator
    {
        public void ValidateParameters(FileCabinetRecord record)
        {
            if (string.IsNullOrWhiteSpace(record?.FirstName) || record.FirstName.Length < 2 || record.FirstName.Length > 60)
            {
                throw new ArgumentException("First name must be between 2 and 60 characters.");
            }

            if (string.IsNullOrWhiteSpace(record.LastName) || record.LastName.Length < 2 || record.LastName.Length > 60)
            {
                throw new ArgumentException("Last name must be between 2 and 60 characters.");
            }

            var minDate = new DateTime(1950, 1, 1);
            if (record.DateOfBirth < minDate || record.DateOfBirth > DateTime.Now)
            {
                throw new ArgumentException("Date of birth must be between 01-Jan-1950 and today.");
            }

            if (record.Height <= 0 || record.Height > 300)
            {
                throw new ArgumentException("Height must be a positive value and less than 300 cm.");
            }

            if (record.Salary <= 0)
            {
                throw new ArgumentException("Salary must be a positive value.");
            }

            if (!"MFN".Contains(record.Gender, StringComparison.Ordinal))
            {
                throw new ArgumentException("Gender must be 'M', 'F', or 'N'.");
            }
        }
    }
}