using System.Globalization;
using System.Xml;
using FileCabinetApp.Models;

namespace FileCabinetApp.Writers
{
    public class FileCabinetRecordXmlWriter
    {
        private readonly XmlWriter writer;

        public FileCabinetRecordXmlWriter(XmlWriter writer)
        {
            this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        public void Write(FileCabinetRecord record)
        {
            ArgumentNullException.ThrowIfNull(record);

            this.writer.WriteStartElement("record");
            this.writer.WriteAttributeString("id", record.Id.ToString(CultureInfo.InvariantCulture));

            this.writer.WriteStartElement("name");
            this.writer.WriteAttributeString("first", record.FirstName);
            this.writer.WriteAttributeString("last", record.LastName);
            this.writer.WriteEndElement();

            this.writer.WriteStartElement("dateOfBirth");
            this.writer.WriteString(record.DateOfBirth.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            this.writer.WriteEndElement();

            this.writer.WriteStartElement("height");
            this.writer.WriteString(record.Height.ToString(CultureInfo.InvariantCulture));
            this.writer.WriteEndElement();

            this.writer.WriteStartElement("salary");
            this.writer.WriteString(record.Salary.ToString(CultureInfo.InvariantCulture));
            this.writer.WriteEndElement();

            this.writer.WriteStartElement("gender");
            this.writer.WriteString(record.Gender.ToString(CultureInfo.InvariantCulture));
            this.writer.WriteEndElement();

            this.writer.WriteEndElement();
        }
    }
}