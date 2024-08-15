using MediaOrganizer.Models;

namespace MediaOrganizerTest
{
    public class SqlDatabaseTests
    {
        [Fact]
        public void TestAddItem()
        {
            SqlDatabaseItem expected = new SqlDatabaseItem { Path = "Bababooey", Modified = 1, Size = 289, Type = "mp4", Name = "NameUwau" };

            SqlDatabase sqlDatabase = new SqlDatabase("TestAddItem");
            sqlDatabase.CreateSqliteDatabase();

            sqlDatabase.AddItemToDatabase(expected);
            var items = sqlDatabase.GetDatabaseItems();

            Assert.Single(items);

            SqlDatabaseItem actual = items.First();

            Assert.Equal(expected, actual);

            sqlDatabase.DeleteTempDb();
        }

        [Fact]
        public void TestRemoveItem()
        {
            SqlDatabaseItem addedItem = new SqlDatabaseItem { Path = "Bababooey", Modified = 1, Size = 289, Type = "mp4", Name = "NameUwau" };

            SqlDatabase sqlDatabase = new SqlDatabase("TestRemoveItem");
            sqlDatabase.CreateSqliteDatabase();

            sqlDatabase.AddItemToDatabase(addedItem);
            var items = sqlDatabase.GetDatabaseItems();

            SqlDatabaseItem actual = items.First();
            Assert.Equal(addedItem, actual);

            sqlDatabase.RemoveItemFromDatabase(addedItem.Path);
            items = sqlDatabase.GetDatabaseItems();

            Assert.Empty(items);

            sqlDatabase.DeleteTempDb();
        }

        [Fact]
        public void TestSortBySize()
        {
            SqlDatabaseItem expected = new SqlDatabaseItem { Path = "Bababooey2", Modified = 1, Size = 999, Type = "mp4", Name = "NameUwau" };
            
            SqlDatabaseItem smaller = new SqlDatabaseItem { Path = "Bababooey1", Modified = 1, Size = 289, Type = "mp4", Name = "NameUwau" };
            SqlDatabaseItem smallest = new SqlDatabaseItem { Path = "Bababooey3", Modified = 1, Size = 102, Type = "mp4", Name = "NameUwau" };

            SqlDatabase sqlDatabase = new SqlDatabase("TestSortBySize");
            sqlDatabase.CreateSqliteDatabase();

            sqlDatabase.AddItemToDatabase(smaller);
            sqlDatabase.AddItemToDatabase(expected);
            sqlDatabase.AddItemToDatabase(smallest);

            var items = sqlDatabase.GetDatabaseItems(filterArg: null, sortType: SqlDatabase.SqlDatabaseSortMode.Size, descending: true);

            SqlDatabaseItem actual = items.First();
            Assert.Equal(expected, actual);

            sqlDatabase.DeleteTempDb();
        }

        [Fact]
        public void TestSortByModified()
        {
            SqlDatabaseItem expected = new SqlDatabaseItem { Path = "Bababooey2", Modified = 99, Size = 999, Type = "mp4", Name = "NameUwau" };

            SqlDatabaseItem smaller = new SqlDatabaseItem { Path = "Bababooey1", Modified = 1, Size = 289, Type = "mp4", Name = "NameUwau" };
            SqlDatabaseItem smallest = new SqlDatabaseItem { Path = "Bababooey3", Modified = 2, Size = 102, Type = "mp4", Name = "NameUwau" };

            SqlDatabase sqlDatabase = new SqlDatabase("TestSortByModified");
            sqlDatabase.CreateSqliteDatabase();

            sqlDatabase.AddItemToDatabase(smaller);
            sqlDatabase.AddItemToDatabase(expected);
            sqlDatabase.AddItemToDatabase(smallest);

            var items = sqlDatabase.GetDatabaseItems(filterArg: null, sortType: SqlDatabase.SqlDatabaseSortMode.Modified, descending: true);

            SqlDatabaseItem actual = items.First();
            Assert.Equal(expected, actual);

            sqlDatabase.DeleteTempDb();
        }

        [Fact]
        public void TestSortByPath()
        {
            SqlDatabaseItem expected = new SqlDatabaseItem { Path = "Bababooey5", Modified = 99, Size = 999, Type = "mp4", Name = "NameUwau" };

            SqlDatabaseItem smaller = new SqlDatabaseItem { Path = "Bababooey3", Modified = 1, Size = 289, Type = "mp4", Name = "NameUwau" };
            SqlDatabaseItem smallest = new SqlDatabaseItem { Path = "Bababooey1", Modified = 2, Size = 102, Type = "mp4", Name = "NameUwau" };

            SqlDatabase sqlDatabase = new SqlDatabase("TestSortByPath");
            sqlDatabase.CreateSqliteDatabase();

            sqlDatabase.AddItemToDatabase(smaller);
            sqlDatabase.AddItemToDatabase(expected);
            sqlDatabase.AddItemToDatabase(smallest);

            var items = sqlDatabase.GetDatabaseItems(filterArg: null, sortType: SqlDatabase.SqlDatabaseSortMode.Filename, descending: true);

            SqlDatabaseItem actual = items.First();
            Assert.Equal(expected, actual);

            sqlDatabase.DeleteTempDb();
        }

        [Fact]
        public void TestAddTagToItem()
        {
            SqlDatabaseItem expected = new SqlDatabaseItem { Path = "Bababooey1", Modified = 1, Size = 999, Type = "mp4", Name = "NameUwau" };
            string expectedTag = "cool-item";
            SqlDatabaseItem untagged = new SqlDatabaseItem { Path = "Bababooey2", Modified = 1, Size = 102, Type = "mp4", Name = "NameUwau" };

            SqlDatabase sqlDatabase = new SqlDatabase("TestAddTagToItem");
            sqlDatabase.CreateSqliteDatabase();

            sqlDatabase.AddItemToDatabase(expected);
            sqlDatabase.AddItemToDatabase(untagged);

            sqlDatabase.AddTagToDatabase(expectedTag);

            sqlDatabase.AddTagToItem(expected.Path, expectedTag);

            var items = sqlDatabase.GetDatabaseItems
            (
                filterArg: new SqlDatabase.SqlDatabaseFilter 
                { 
                    Tags=new List<string> { expectedTag } 
                }, 
                sortType: SqlDatabase.SqlDatabaseSortMode.Size, descending: true
            );

            SqlDatabaseItem actual = items.First();
            Assert.Equal(expected, actual);

            Assert.Single(items);

            sqlDatabase.DeleteTempDb();
        }

        [Fact]
        public void TestRemoveTagFromItem()
        {
            SqlDatabaseItem expected = new SqlDatabaseItem { Path = "Bababooey1", Modified = 1, Size = 999, Type = "mp4", Name = "NameUwau" };
            string expectedTag = "cool-item";

            SqlDatabase sqlDatabase = new SqlDatabase("TestRemoveTagFromItem");
            sqlDatabase.CreateSqliteDatabase();

            sqlDatabase.AddItemToDatabase(expected);

            sqlDatabase.AddTagToDatabase(expectedTag);

            sqlDatabase.AddTagToItem(expected.Path, expectedTag);

            var items = sqlDatabase.GetDatabaseItems
            (
                filterArg: new SqlDatabase.SqlDatabaseFilter
                {
                    Tags = new List<string> { expectedTag }
                },
                sortType: SqlDatabase.SqlDatabaseSortMode.Size, descending: true
            );

            SqlDatabaseItem actual = items.First();

            sqlDatabase.RemoveTagFromItem(expected.Path, expectedTag);
            List<string> actualTags = sqlDatabase.GetItemTags(expected.Path);

            Assert.Equal(expected, actual);
            Assert.Empty(actualTags);

            sqlDatabase.DeleteTempDb();
        }

        [Fact]
        public void TestGetItemTags()
        {
            List<string> expected = new List<string> { "cool", "new", "tags!" };
            
            SqlDatabase sqlDatabase = new SqlDatabase("TestGetItemTags");
            sqlDatabase.CreateSqliteDatabase();
            
            SqlDatabaseItem dbItem = new SqlDatabaseItem { Path = "Bababooey", Modified = 1, Size = 999, Type = "mp4", Name = "NameUwau" };
            sqlDatabase.AddItemToDatabase(dbItem);

            foreach (string tag in expected)
            {
                sqlDatabase.AddTagToDatabase(tag);
                sqlDatabase.AddTagToItem(dbItem.Path, tag);
            }

            List<string> actual = sqlDatabase.GetItemTags(dbItem.Path);

            Assert.Equal(expected, actual);

            sqlDatabase.DeleteTempDb();
        }

        [Fact]
        public void TestGetTagsInDb()
        {
            List<string> expected = new List<string> { "cool", "new", "tags!" };

            SqlDatabase sqlDatabase = new SqlDatabase("TestGetTagsInDb");
            sqlDatabase.CreateSqliteDatabase();

            foreach (string tag in expected)
            {
                sqlDatabase.AddTagToDatabase(tag);
            }

            List<string> actual = sqlDatabase.GetTagsInDatabase();

            Assert.Equal(expected, actual);

            sqlDatabase.DeleteTempDb();
        }

        [Fact]
        public void TestRemoveTag()
        {
            List<string> expected = new List<string> { "cool", "tags!" };
            string badTag = "new";
            List<string> total = new List<string> { "cool", badTag, "tags!" };

            SqlDatabase sqlDatabase = new SqlDatabase("TestRemoveTag");
            sqlDatabase.CreateSqliteDatabase();

            SqlDatabaseItem dbItem = new SqlDatabaseItem { Path = "Bababooey", Modified = 1, Size = 999, Type = "mp4", Name = "NameUwau" };
            sqlDatabase.AddItemToDatabase(dbItem);

            foreach (string tag in total)
            {
                sqlDatabase.AddTagToDatabase(tag);
                sqlDatabase.AddTagToItem(dbItem.Path, tag);
            }

            sqlDatabase.RemoveTagFromDatabase(badTag);

            List<string> tagsOnItem = sqlDatabase.GetItemTags(dbItem.Path);
            List<string> actual = sqlDatabase.GetTagsInDatabase();

            Assert.Equal(expected, actual);
            Assert.DoesNotContain(badTag, tagsOnItem);

            sqlDatabase.DeleteTempDb();
        }
    }
}