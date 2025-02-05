namespace FileCabinetApp.Models
{
    public class FileCabinetRecord
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }

        public short Height { get; set; }

        public decimal Salary { get; set; }

        public char Gender { get; set; }
    }
}