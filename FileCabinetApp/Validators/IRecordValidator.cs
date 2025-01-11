using FileCabinetApp.Models;

namespace FileCabinetApp.Validators
{
    public interface IRecordValidator
    {
        void ValidateParameters(FileCabinetRecord record);
    }
}