using System.IO;
using FileCabinetApp.Models;

namespace FileCabinetApp.Writers
{
    public class FileCabinetRecordCsvWriter
    {
        private readonly TextWriter writer;

        public FileCabinetRecordCsvWriter(TextWriter writer)
        {
            this.writer = writer;
        }

        public void Write(FileCabinetRecord record)
        {
            ArgumentNullException.ThrowIfNull(record, nameof(record));
            this.writer.WriteLine($"{record.Id},{record.FirstName},{record.LastName},{record.DateOfBirth:MM/dd/yyyy},{record.Height},{record.Salary},{record.Gender}");
        }

        public void WriteHeader()
        {
            this.writer.WriteLine("Id,FirstName,LastName,DateOfBirth,Height,Salary,Gender");
        }
    }
}