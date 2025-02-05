using System;
using System.Collections.ObjectModel;
using FileCabinetApp.Models;

namespace FileCabinetApp.Services
{
    public interface IFileCabinetService
    {
        int CreateRecord(
            string firstName,
            string lastName,
            DateTime dateOfBirth,
            short height,
            decimal salary,
            char gender);

        void EditRecord(
            int id,
            string firstName,
            string lastName,
            DateTime dateOfBirth,
            short height,
            decimal salary,
            char gender);

        ReadOnlyCollection<FileCabinetRecord> GetRecords();

        int GetStat();

        ReadOnlyCollection<FileCabinetRecord> FindByFirstName(string firstName);

        ReadOnlyCollection<FileCabinetRecord> FindByLastName(string lastName);

        ReadOnlyCollection<FileCabinetRecord> FindByDateOfBirth(DateTime dateOfBirth);

        FileCabinetServiceSnapshot MakeSnapshot();
    }
}
