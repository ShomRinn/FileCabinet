using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using FileCabinetApp.Models;

namespace FileCabinetApp.Readers
{
    /// <summary>
    /// Reads FileCabinetRecord objects from a custom XML format (manually).
    /// </summary>
    public class FileCabinetRecordXmlReader
    {
        private readonly XmlReader xmlReader;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileCabinetRecordXmlReader"/> class.
        /// </summary>
        /// <param name="xmlReader">The XML reader.</param>
        public FileCabinetRecordXmlReader(XmlReader xmlReader)
        {
            this.xmlReader = xmlReader ?? throw new ArgumentNullException(nameof(xmlReader));
        }

        /// <summary>
        /// Reads all records from the custom XML format and returns a list of FileCabinetRecord objects.
        /// </summary>
        /// <returns>A list of parsed records.</returns>
        public IList<FileCabinetRecord> ReadAll()
        {
            var result = new List<FileCabinetRecord>();

            // We expect <records> as the root element
            while (this.xmlReader.Read())
            {
                // Move to each <record> element
                if (this.xmlReader.NodeType == XmlNodeType.Element && this.xmlReader.Name.Equals("record", StringComparison.OrdinalIgnoreCase))
                {
                    var record = ParseRecordElement(this.xmlReader);
                    if (record != null)
                    {
                        result.Add(record);
                    }
                }
            }

            return result;
        }

        private static FileCabinetRecord? ParseRecordElement(XmlReader reader)
        {
            if (reader.GetAttribute("id") == null)
            {
                Console.WriteLine("XML parse warning: <record> element has no 'id' attribute. Skipped.");
                SkipCurrentElement(reader);
                return null;
            }

            if (!int.TryParse(reader.GetAttribute("id"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
            {
                Console.WriteLine($"XML parse warning: invalid record id '{reader.GetAttribute("id")}'. Skipped.");
                SkipCurrentElement(reader);
                return null;
            }

            var record = new FileCabinetRecord
            {
                Id = id
            };

            // Move deeper into the <record> element
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name.Equals("record", StringComparison.OrdinalIgnoreCase))
                {
                    // End of this record
                    break;
                }

                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name.ToLowerInvariant())
                    {
                        case "name":
                            var firstName = reader.GetAttribute("first") ?? string.Empty;
                            var lastName = reader.GetAttribute("last") ?? string.Empty;
                            record.FirstName = firstName;
                            record.LastName = lastName;
                            break;

                        case "dateofbirth":
                            record.DateOfBirth = ReadDateOfBirth(reader);
                            break;

                        case "height":
                            record.Height = ReadShortValue(reader, "height");
                            break;

                        case "salary":
                            record.Salary = ReadDecimalValue(reader, "salary");
                            break;

                        case "gender":
                            record.Gender = ReadGenderValue(reader);
                            break;

                        default:
                            // Unknown element, skip it
                            SkipCurrentElement(reader);
                            break;
                    }
                }
            }

            return record;
        }

        private static DateTime ReadDateOfBirth(XmlReader reader)
        {
            // Move to text inside <dateOfBirth>...</dateOfBirth>
            if (!reader.Read() || reader.NodeType != XmlNodeType.Text)
            {
                return default;
            }

            string content = reader.Value.Trim();
            if (DateTime.TryParse(content, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dob))
            {
                return dob;
            }

            return default;
        }

        private static short ReadShortValue(XmlReader reader, string elementName)
        {
            if (!reader.Read() || reader.NodeType != XmlNodeType.Text)
            {
                return 0;
            }

            string content = reader.Value.Trim();
            if (short.TryParse(content, NumberStyles.Integer, CultureInfo.InvariantCulture, out short val))
            {
                return val;
            }
            return 0;
        }

        private static decimal ReadDecimalValue(XmlReader reader, string elementName)
        {
            if (!reader.Read() || reader.NodeType != XmlNodeType.Text)
            {
                return 0m;
            }

            string content = reader.Value.Trim();
            if (decimal.TryParse(content, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal val))
            {
                return val;
            }
            return 0m;
        }

        private static char ReadGenderValue(XmlReader reader)
        {
            if (!reader.Read() || reader.NodeType != XmlNodeType.Text)
            {
                return '\0';
            }

            string content = reader.Value.Trim();
            if (content.Length > 0)
            {
                return content[0];
            }

            return '\0';
        }

        private static void SkipCurrentElement(XmlReader reader)
        {
            if (reader.IsEmptyElement)
            {
                return;
            }

            int depth = reader.Depth;
            while (reader.Read() && !(reader.Depth == depth && reader.NodeType == XmlNodeType.EndElement))
            {
                // Keep reading until we exit the current element
            }
        }
    }
}