using System.Collections.ObjectModel;
using System.IO;
using FileCabinetApp.Models;
using FileCabinetApp.Writers;

namespace FileCabinetApp.Services
{
    public class FileCabinetServiceSnapshot
    {
        private readonly ReadOnlyCollection<FileCabinetRecord> records;

        public FileCabinetServiceSnapshot(ReadOnlyCollection<FileCabinetRecord> records)
        {
            ArgumentNullException.ThrowIfNull(records);
            this.records = records;
        }

        /// <summary>
        /// Saves records to CSV format.
        /// </summary>
        /// <param name="writer">StreamWriter to write the data.</param>
        public void SaveToCsv(StreamWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);

            var csvWriter = new FileCabinetRecordCsvWriter(writer);
            foreach (var record in this.records)
            {
                csvWriter.Write(record);
            }
        }

        /// <summary>
        /// Saves records to XML format.
        /// </summary>
        /// <param name="writer">XmlWriter to write the data.</param>
        public void SaveToXml(System.Xml.XmlWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);

            var xmlWriter = new FileCabinetRecordXmlWriter(writer);
            writer.WriteStartDocument();
            writer.WriteStartElement("records");

            foreach (var record in this.records)
            {
                xmlWriter.Write(record);
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }
    }
}