using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaOrganizer.Models
{
    /// <summary>
    /// Type with all fields of a database media item.
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
}
