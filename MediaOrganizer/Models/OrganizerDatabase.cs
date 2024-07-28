using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MediaOrganizer.Models
{
    /// <summary>
    /// Type for organizer database media items
    /// </summary>
    internal struct DatabaseItem
    {
        /// <summary>
        /// Unique hashed ID of the database item
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Name of media file
        /// </summary>
        public string Filename { get; set; }
        
        /// <summary>
        /// Name of media type (not necessarily in database types)
        /// </summary>
        public string Type { get; set; }    
        
        /// <summary>
        /// List of media tags
        /// </summary>
        public List<string> Tags { get; set; }
        
        /// <summary>
        /// Path to media thumbnail (optional)
        /// </summary>
        public string? ThumbPath { get; set; }
    }

    /// <summary>
    /// Type for media item groups
    /// </summary>
    internal struct DatabaseGroup
    {
        /// <summary>
        /// Name of the group
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Description of the group (optional)
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// List of items in the group
        /// </summary>
        public List<int> Items { get; set; }

        /// <summary>
        /// Icon to display next to the group (optional)
        /// </summary>
        public string? Icon { get; set; }
    }

    /// <summary>
    /// Class containing media organizer database and methods to interact with and (de)serialize it
    /// </summary>
    internal class OrganizerDatabase
    {
        #region Fields

        /// <summary>
        /// Map of database items to their id
        /// </summary>
        Dictionary<string, DatabaseItem> _databaseItems = new Dictionary<string, DatabaseItem> ();
        
        /// <summary>
        /// List of allowed types
        /// </summary>
        List<string> _allowedTypes = new List<string>();
        
        /// <summary>
        /// List of available groups
        /// </summary>
        List<DatabaseGroup> _groups = new List<DatabaseGroup>();

        /// <summary>
        /// List of locations to scan
        /// </summary>
        List<string> _scanLocations = new List<string>();

        #endregion

        #region Properties

        /// <summary>
        /// Map of database items to their id
        /// </summary>
        public Dictionary<string, DatabaseItem> Items
        {
            get => _databaseItems;
            set => _databaseItems = value;
        }

        /// <summary>
        /// List of filetypes allowed in the database
        /// </summary>
        public List<string> AllowedTypes
        {
            get => _allowedTypes;
            set => _allowedTypes = value;
        }

        /// <summary>
        /// List of database groups
        /// </summary>
        public List<DatabaseGroup> Groups
        {
            get => _groups;
            set => _groups = value;
        }

        /// <summary>
        /// List of locations for the database to scan for changes
        /// </summary>
        public List<string> ScanLocations
        {
            get => _scanLocations;
            set => _scanLocations = value;
        }

        #endregion

        #region Serialization

        public void SerializeToFile(Stream file)
        {
            using StreamWriter writer = new StreamWriter(file);
            XmlSerializer serializer = new XmlSerializer(typeof(DatabaseItem));

            serializer.Serialize(writer, this);
        }

        public static OrganizerDatabase DeserializeFromDatabaseFile(Stream file)
        {
            using StreamReader reader = new StreamReader(file);
            XmlSerializer serializer = new XmlSerializer(typeof(OrganizerDatabase));

            object? deserialized = serializer.Deserialize(file);
            if (deserialized is OrganizerDatabase database)
            {
                return database;
            }

            return new OrganizerDatabase();
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Scans all directories in ScanLocations for new content
        /// </summary>
        /// <returns>Number of new items found and added to database</returns>
        public int Scan()
        {
            int newItemsCount = 0;

            foreach (string location in _scanLocations)
            {
                List<string> locations = new List<string>();

                foreach (string file in _allowedTypes.SelectMany(type => Directory.EnumerateFiles(location, $"*.{type}", SearchOption.AllDirectories)))
                {
                    using Stream stream = File.OpenRead(file);
                    byte[]? hashBytes = MD5.Create().ComputeHash(stream);
                    if (hashBytes != null)
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (byte b in hashBytes)
                        {
                            sb.Append(b.ToString("X2"));
                        }

                        string hashStr = sb.ToString();
                        if (!_databaseItems.ContainsKey(hashStr))
                        {
                            DatabaseItem dbItem;
                            if (Directory.Exists($"{file}.metadata")) 
                            {
                                using StreamReader metadataReader = new StreamReader($"{file}.metadata");
                                XmlSerializer metadataSerializer = new XmlSerializer(typeof(DatabaseItem));
                                if (metadataSerializer.Deserialize(metadataReader) is DatabaseItem item)
                                {
                                    dbItem = item;
                                }
                                else
                                {

                                    dbItem = new DatabaseItem { Filename = file, Id = hashStr, Type = GetFiletypeFromFilename(file)};
                                }
                            }
                            else
                            {
                                dbItem = new DatabaseItem { Filename = file, Id = hashStr, Type = GetFiletypeFromFilename(file) };
                            }
                            _databaseItems.Add(hashStr, dbItem);
                        }
                    }
                }
            }

            return newItemsCount;
        }

        #endregion

        #region Helper methods

        string GetFiletypeFromFilename(string filename)
        {
            return filename.Split('.').Last();
        }
        
        #endregion
    }
}
