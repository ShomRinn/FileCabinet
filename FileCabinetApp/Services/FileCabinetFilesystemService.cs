using System;
using System.Collections.ObjectModel;
using System.IO;
using FileCabinetApp.Models;
using FileCabinetApp.Validators;

namespace FileCabinetApp.Services
{
    public class FileCabinetFilesystemService : IFileCabinetService
    {
        private readonly FileStream fileStream;
        private readonly IRecordValidator validator;

        public FileCabinetFilesystemService(FileStream fileStream, IRecordValidator validator)
        {
            this.fileStream = fileStream ?? throw new ArgumentNullException(nameof(fileStream));
            this.validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public int CreateRecord(
            string firstName,
            string lastName,
            DateTime dateOfBirth,
            short height,
            decimal salary,
            char gender)
        {
            // TODO: Реализовать логику записи в бинарный файл
            throw new NotImplementedException();
        }

        public void EditRecord(
            int id,
            string firstName,
            string lastName,
            DateTime dateOfBirth,
            short height,
            decimal salary,
            char gender)
        {
            throw new NotImplementedException();
        }

        public ReadOnlyCollection<FileCabinetRecord> GetRecords()
        {
            throw new NotImplementedException();
        }

        public int GetStat()
        {
            throw new NotImplementedException();
        }

        public ReadOnlyCollection<FileCabinetRecord> FindByFirstName(string firstName)
        {
            throw new NotImplementedException();
        }

        public ReadOnlyCollection<FileCabinetRecord> FindByLastName(string lastName)
        {
            throw new NotImplementedException();
        }

        public ReadOnlyCollection<FileCabinetRecord> FindByDateOfBirth(DateTime dateOfBirth)
        {
            throw new NotImplementedException();
        }

        public FileCabinetServiceSnapshot MakeSnapshot()
        {
            throw new NotImplementedException();
        }
    }
}