using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using FileCabinetApp.Models;
using FileCabinetApp.Writers;
using FileCabinetApp.Readers;

namespace FileCabinetApp.Services
{
    /// <summary>
    /// Represents a snapshot of FileCabinet records.
    /// </summary>
    public class FileCabinetServiceSnapshot
    {
        private FileCabinetRecord[] records;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileCabinetServiceSnapshot"/> class.
        /// </summary>
        /// <param name="records">The initial records.</param>
        public FileCabinetServiceSnapshot(ReadOnlyCollection<FileCabinetRecord> records)
        {
            if (records == null)
            {
                throw new ArgumentNullException(nameof(records));
            }

            this.records = records.ToArray();
        }

        /// <summary>
        /// Gets the records contained in this snapshot.
        /// </summary>
        public ReadOnlyCollection<FileCabinetRecord> Records
        {
            get { return new ReadOnlyCollection<FileCabinetRecord>(this.records); }
        }

        /// <summary>
        /// Saves all records to a CSV format.
        /// </summary>
        /// <param name="writer">The target stream writer.</param>
        public void SaveToCsv(StreamWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            var csvWriter = new FileCabinetRecordCsvWriter(writer);

            // Optional: write a header if needed
            // csvWriter.WriteHeader();

            foreach (var record in this.records)
            {
                csvWriter.Write(record);
            }
        }

        /// <summary>
        /// Saves all records to an XML format.
        /// </summary>
        /// <param name="writer">The target XML writer.</param>
        public void SaveToXml(System.Xml.XmlWriter writer)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

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

        /// <summary>
        /// Loads records from a CSV stream.
        /// </summary>
        /// <param name="reader">The source stream reader for CSV.</param>
        public void LoadFromCsv(StreamReader reader)
        {
            if (reader is null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            var csvReader = new FileCabinetRecordCsvReader(reader);
            var list = csvReader.ReadAll();
            this.records = list.ToArray();
        }

        /// <summary>
        /// Loads records from an XML stream.
        /// </summary>
        /// <param name="stream">The source stream for XML.</param>
        public void LoadFromXml(Stream stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var serializer = new XmlSerializer(typeof(FileCabinetRecordsContainer));
            var container = (FileCabinetRecordsContainer?)serializer.Deserialize(stream);

            this.records = container?.Records ?? Array.Empty<FileCabinetRecord>();
        }
    }

    /// <summary>
    /// A container class for XML serialization.
    /// </summary>
    [XmlRoot("records")]
    public class FileCabinetRecordsContainer
    {
        /// <summary>
        /// Gets or sets the collection of records.
        /// </summary>
        [XmlElement("record")]
        public FileCabinetRecord[] Records { get; set; }
    }
}