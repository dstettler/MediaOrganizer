using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MediaOrganizer.Models
{
    public class SqlDatabase
    {
        #region Enums and structs

        /// <summary>
        /// Database item
        /// </summary>
        public struct SqlDatabaseItem
        {

            /// <summary>
            /// Filepath of the item
            /// </summary>
            public required string Path { get; set; }

            /// <summary>
            /// Filetype of the item
            /// </summary>
            public required string Type { get; set; }
            
            /// <summary>
            /// Filesize of the item
            /// </summary>
            public required int Size { get; set; }
            
            /// <summary>
            /// Unix timecode of when the file was modified
            /// </summary>
            public required int Modified { get; set; }          
            
            /// <summary>
            /// In-organizer name of the file
            /// </summary>
            public string? Name { get; set; }
            
            /// <summary>
            /// In-organizer description of the file
            /// </summary>
            public string? Description { get; set; }
        }

        public struct SqlDatabaseCollection
        {
            public string Name { get; set; }
            public string Icon { get; set; }
            public string Description { get; set; }
            public int LastUpdated { get; set; }
        }

        public struct SqlDatabaseFilter
        {
            public List<string>? Filenames { get; set; }
            public List<string>? Tags { get; set; }
        }

        /// <summary>
        /// Type with which to sort outputs
        /// </summary>
        public enum SqlDatabaseSortMode
        {
            /// <summary>
            /// Date last modified (on time of database update)
            /// </summary>
            Modified,
            
            /// <summary>
            /// Alphabetical order
            /// </summary>
            Filename,

            /// <summary>
            /// Filesize (on time of database update)
            /// </summary>
            Size
        }

        #endregion

        #region Constants

        public const string ORGANIZER_DATABASE_NAME = "organizer.db";

        /// <summary>
        /// SQL query to create the item table
        /// </summary>
        private const string CREATE_ITEMS_TABLE = @"
CREATE TABLE MediaItems (
    Path		TEXT,
	Size		INTEGER,
	Modified	INTEGER,
	Type		TEXT,
    Name        TEXT,
    Description TEXT,
	PRIMARY KEY(Path)
);";

        /// <summary>
        /// SQL query to create the collection table
        /// </summary>
        private const string CREATE_COLLECTIONS_TABLE = @"
CREATE TABLE Collections (
	Name			TEXT,
	Icon			TEXT,
	Description		TEXT,
	LastUpdated		INTEGER,
	PRIMARY KEY(Name)
);";

        /// <summary>
        /// SQL query to create the tags table
        /// </summary>
        private const string CREATE_TAGS_TABLE = @"	
CREATE TABLE Tags (
	Id		INTEGER,
	Name	TEXT,
	PRIMARY KEY(Id AUTOINCREMENT)
);
INSERT INTO Tags (Name) VALUES ('media');
";

        /// <summary>
        /// SQL query to create the tag:item table
        /// </summary>
        private const string CREATE_ITEM_TAGS_TABLE = @"
CREATE TABLE ItemTags (
	TagId	INTEGER,
	Item	TEXT
);";

        /// <summary>
        /// SQL query to create the collection:item table
        /// </summary>
        private const string CREATE_COLLECTION_ITEMS_TABLE = @"
CREATE TABLE CollectionItems (
	Collection	TEXT,
	Item	    TEXT
);";

        /// <summary>
        /// SQL query to create the tag:collection table
        /// </summary>
        private const string CREATE_COLLECTION_TAGS_TABLE = @"
CREATE TABLE CollectionTags (
	TagId		INTEGER,
	Collection	TEXT
);";

        private const string GET_ITEMS_BY_CONDITION = @"
SELECT mitems.*
FROM Tags t
JOIN ItemTags itags ON itags.TagId = t.Id
JOIN MediaItems mitems ON mitems.Path = itags.Item";

        /// <summary>
        /// SQL query to add item to the MediaItems table.
        /// </summary>
        private const string ADD_ITEM = @"
INSERT INTO MediaItems(Path, Size, Modified, Type, Name, Description) VALUES ($path, $size, $modified, $type, $name, $description);
INSERT INTO ItemTags(TagId,Item) VALUES (1, $path);
";

        /// <summary>
        /// SQL query to remove item from any table it may be referenced in.
        /// </summary>
        private const string DELETE_ITEM = @"
DELETE FROM MediaItems WHERE Path=$path;
DELETE FROM ItemTags WHERE Item=$path;
DELETE FROM CollectionItems WHERE Item=$path;
";

        /// <summary>
        /// SQL query to add new tag to the tags table.
        /// </summary>
        private const string ADD_TAG = @"
INSERT INTO Tags (Name) VALUES ($tag)
";

        /// <summary>
        /// SQL query to add tag to item
        /// </summary>
        private const string ADD_TAG_TO_ITEM = @"
INSERT INTO ItemTags (TagId, Item) VALUES ((SELECT Tags.Id FROM Tags WHERE (Name = $tag)), $item)
";

        /// <summary>
        /// SQL querty to delete a tag from the tags table.
        /// </summary>
        private const string DELETE_TAG = @"
DELETE FROM Tags WHERE Name=$tag;
DELETE FROM ItemTags WHERE TagId=(SELECT t.Id
FROM Tags t
WHERE t.Name = $tag);
";

        private const string GET_ITEM_TAGS = @"
SELECT DISTINCT t.Name
FROM Tags t
JOIN ItemTags itags ON itags.TagId = t.Id
JOIN MediaItems mitems ON mitems.Path = $path
WHERE t.Id <> 1
";

        private const string GET_TAGS_IN_DB = @"
SELECT DISTINCT t.Name
FROM Tags t
WHERE t.Id <> 1
";

        #endregion

        #region Fields

        /// <summary>
        /// Path to the temporary SQLite db file
        /// </summary>
        private readonly string _dbPath;

        private List<string> _tags = new List<string>();
        private List<string> _names = new List<string>();

        #endregion

        #region Properties

        /// <summary>
        /// Property to get the temporary database's path
        /// </summary>
        public string TempDatabasePath
        {
            get => _dbPath;
        }

        #endregion

        #region Constructor and public methods

        /// <summary>
        /// Creates a reference to temporary database.
        /// </summary>
        /// <param name="tempNameBase"></param>
        public SqlDatabase(string tempNameBase, ZipArchiveEntry? zipArchiveEntry = null)
        {
            _dbPath = Path.Join(Path.GetTempPath(), $"{tempNameBase}.tempdb");
            
            zipArchiveEntry?.ExtractToFile(_dbPath);
        }

        public void CreateSqliteDatabase()
        {
            DeleteTempDb();

            string query = CREATE_ITEMS_TABLE +
                CREATE_COLLECTIONS_TABLE +
                CREATE_TAGS_TABLE +
                CREATE_ITEM_TAGS_TABLE +
                CREATE_COLLECTION_ITEMS_TABLE +
                CREATE_COLLECTION_TAGS_TABLE;

            ExecuteSqlNonQuery(query, null);
        }

        /// <summary>
        /// Deletes the .tempdb file without writing the final db updates.
        /// </summary>
        public void DeleteTempDb()
        {
            // Ensure all connections have been fully closed before attempting to delete the db
            SqliteConnection.ClearAllPools();

            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }

        /// <summary>
        /// Adds item to the database.
        /// </summary>
        /// <param name="item">SqlDatabaseItem to add.</param>
        public void AddItemToDatabase(SqlDatabaseItem item)
        {
            lock (this)
            {
                ExecuteSqlNonQuery(ADD_ITEM, new Dictionary<string, object?>
                {
                    { "$path", item.Path },
                    { "$size", item.Size },
                    { "$modified", item.Modified },
                    { "$type", item.Type },
                    { "$name", item.Name as object ?? DBNull.Value },
                    { "$description", item.Description as object ?? DBNull.Value },
                });
            }
        }

        /// <summary>
        /// Deletes item from the database.
        /// </summary>
        /// <param name="key">SqlDatabaseItem path to delete.</param>
        public void RemoveItemFromDatabase(string key)
        {
            lock (this)
            {
                ExecuteSqlNonQuery(DELETE_ITEM, new Dictionary<string, object?>
                {
                    { "$path", key },
                });
            }
        }

        public void AddTagToItem(string itemPath, string tag)
        {
            lock (this)
            {
                ExecuteSqlNonQuery(ADD_TAG_TO_ITEM, new Dictionary<string, object?>
                {
                    { "$tag", tag },
                    { "$item", itemPath }
                });
            }
        }

        public void AddTagToDatabase(string tag)
        {
            lock (this)
            {
                ExecuteSqlNonQuery(ADD_TAG, new Dictionary<string, object?>
                {
                    { "$tag", tag }
                });
            }
        }

        public void RemoveTagFromDatabase(string tag) 
        {
            lock (this)
            {
                ExecuteSqlNonQuery(DELETE_TAG, new Dictionary<string, object?>
                {
                    { "$tag", tag }
                });
            }
        }

        public SqlDatabaseFilter GetFilterFromString(string queryString)
        {
            throw new NotImplementedException();
        }

        // TODO
        public List<SqlDatabaseCollection> GetDatabaseCollections()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get database MediaItems, with optional filtering and sorting.
        /// </summary>
        /// <param name="filterArg">Filter to apply to search. Defaults to null.</param>
        /// <param name="sortType">Mode to sort the output list. Defualts to Date Modified.</param>
        /// <returns>List of items returned from database based on criteria given.</returns>
        public List<SqlDatabaseItem> GetDatabaseItems(SqlDatabaseFilter? filterArg = null, SqlDatabaseSortMode sortType = SqlDatabaseSortMode.Modified, bool descending = true)
        {
            List<SqlDatabaseItem> items = new List<SqlDatabaseItem>();
            string conditionText = "\n";
            string sortText = sortType switch
            {
                SqlDatabaseSortMode.Modified => "ORDER BY mitems.Modified " + (descending ? "DESC" : "ASC"),
                SqlDatabaseSortMode.Filename => "ORDER BY mitems.Path " + (descending ? "DESC" : "ASC"),
                SqlDatabaseSortMode.Size => "ORDER BY mitems.Size " + (descending ? "DESC" : "ASC"),
                _ => ""
            };

            if (filterArg is SqlDatabaseFilter filter)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append("WHERE ");

                if (filter.Tags is List<string> tagsList)
                {
                    foreach (string tag in tagsList)
                    {
                        builder.Append($"t.name = '{tag}' AND ");
                    }
                }

                if (filter.Filenames is List<string> filenamesList)
                {
                    foreach (string filename in filenamesList)
                    {
                        builder.Append($"mitems.path LIKE '{filename}' OR ");
                    }
                }

                conditionText = builder.ToString();
                conditionText = "\n" + conditionText.Trim().Substring(0, conditionText.LastIndexOf("AND"));
            }

            lock (this)
            {
                using SqliteConnection connection = new SqliteConnection($"Data Source={_dbPath}");
                connection.Open();

                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = GET_ITEMS_BY_CONDITION + conditionText + " " + sortText;

                using SqliteDataReader reader = cmd.ExecuteReader();
                
                while (reader.Read())
                {
                    string? name = null;
                    if (!reader.IsDBNull(4))
                    {
                        name = reader.GetString(4);
                    }

                    string? description = null;
                    if (!reader.IsDBNull(5))
                    {
                        description = reader.GetString(5);
                    }

                    SqlDatabaseItem item = new SqlDatabaseItem
                    {
                        Path = reader.GetString(0),
                        Size = reader.GetInt32(1),
                        Modified = reader.GetInt32(2),
                        Type = reader.GetString(3),
                        Name = name,
                        Description = description
                    };

                    items.Add(item);
                }
            }

            return items;
        }

        public List<string> GetItemTags(string itemPath)
        {
            List<string> tags = new List<string>();

            lock (this)
            {
                using SqliteConnection connection = new SqliteConnection($"Data Source={_dbPath}");
                connection.Open();

                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = GET_ITEM_TAGS;

                cmd.Parameters.AddWithValue("$path", itemPath);

                using SqliteDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    tags.Add(reader.GetString(0));
                }
            }

            return tags;
        }

        public List<string> GetTagsInDatabase()
        {
            List<string> tags = new List<string>();

            lock (this)
            {
                using SqliteConnection connection = new SqliteConnection($"Data Source={_dbPath}");
                connection.Open();

                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = GET_TAGS_IN_DB;


                using SqliteDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    tags.Add(reader.GetString(0));
                }
            }

            return tags;
        }

        #endregion

        #region Helper methods

        private void ExecuteSqlNonQuery(string query, Dictionary<string, object?>? parameters)
        {
            using SqliteConnection connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            SqliteCommand command = connection.CreateCommand();
            command.CommandText = query;

            if (parameters is not null)
            {
                foreach (var parameter in parameters)
                {
                    command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                }
            }

            command.ExecuteNonQuery();
        }

        #endregion
    }
}
