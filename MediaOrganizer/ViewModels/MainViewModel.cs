using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaOrganizer.Models;
using System;
using System.IO;
using System.Runtime;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO.Compression;
using Avalonia.Dialogs;
using System.ComponentModel;

namespace MediaOrganizer.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly Window _window;

    /// <summary>
    /// The SQL database
    /// </summary>
    [ObservableProperty]
    private SqlDatabase? _sqlDatabase;

    [ObservableProperty]
    private string? _searchBarText;

    [ObservableProperty]
    private List<SqlDatabaseItem>? _databaseItems;

    [ObservableProperty]
    private SqlDatabaseItem? _selectedDatabaseItem;

    [ObservableProperty]
    private string? _selectedDatabaseItemName;

    [ObservableProperty]
    private string? _selectedDatabaseItemDescription;

    [ObservableProperty]
    private string? _selectedDatabaseItemThumb;

    [ObservableProperty]
    private List<string>? _selectedDatabaseItemTags;

    public MainViewModel(Window window)
    {
        _window = window;
        SqlDatabase = null;
        SearchBarText = null;
        DatabaseItems = null;
        SelectedDatabaseItem = null;
    }

    public MainViewModel()
    {
        _window = new Window();
        SqlDatabase = null;
        SearchBarText = null;
        DatabaseItems = null;
        SelectedDatabaseItem = null;
    }

    partial void OnSelectedDatabaseItemChanged(SqlDatabaseItem? oldValue, SqlDatabaseItem? newValue)
    {
        if (SqlDatabase is SqlDatabase db && newValue is not null)
        {
            SelectedDatabaseItemName = newValue?.Name;
            SelectedDatabaseItemDescription = newValue?.Description;
            SelectedDatabaseItemThumb = newValue?.Thumb;
            SelectedDatabaseItemTags = newValue?.Tags;
        }
    }

    [RelayCommand]
    public async Task HandleOrganizerCreate()
    {
        var file = await _window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { Title = "New Organizer" });
        if (file is not null)
        {
            SqlDatabaseItem expected = new SqlDatabaseItem { Path = "Bababooey", Modified = 1, Size = 289, Type = "mp4", Name = "NameUwau" , Description="Test\n\nTest2\nLong description"};
            SqlDatabaseItem expected2 = new SqlDatabaseItem { Path = "Bababooey 2", Modified = 1, Size = 999, Type = "mp4", Name = "NameUwau 2" };
            SqlDatabase = new SqlDatabase(file.Name);
            SqlDatabase.CreateSqliteDatabase();

            SqlDatabase.AddItemToDatabase(expected);
            SqlDatabase.AddItemToDatabase(expected2);

            SqlDatabase.AddTagToDatabase("cool");
            SqlDatabase.AddTagToDatabase("epic");
            SqlDatabase.AddTagToDatabase("awesome");

            SqlDatabase.AddTagToItem(expected.Path, "cool");
            SqlDatabase.AddTagToItem(expected.Path, "epic");
            SqlDatabase.AddTagToItem(expected2.Path, "awesome");

            await using Stream stream = await file.OpenWriteAsync();
            using StreamWriter writer = new StreamWriter(stream);
            await writer.WriteLineAsync("temp");
        }
    }

    [RelayCommand]
    public async Task HandleDatabaseSelect()
    {
        // Start async operation to open the dialog.
        var file = await _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Organizer",
            AllowMultiple = false
        });

        if (file.Count >= 1)
        {
            ZipArchive zipArchive = ZipFile.OpenRead(file[0].Path.ToString());
            ZipArchiveEntry dbEntry = zipArchive.CreateEntry(SqlDatabase.ORGANIZER_DATABASE_NAME);
            SqlDatabase = new SqlDatabase(file[0].Name, dbEntry);
        }
    }

    [RelayCommand]
    public Task HandleButtonClick()
    {
        SqlDatabase.SqlDatabaseFilter filter = new SqlDatabase.SqlDatabaseFilter();
        filter.Tags = new List<string>() { "owo" };
        SqlDatabase?.GetDatabaseItems(filter);
        return Task.CompletedTask;
    }

    [RelayCommand]
    public Task HandleTagSearched()
    {
        if (SqlDatabase is SqlDatabase db)
        {
            if (SearchBarText is not null)
            {
                SqlDatabase.SqlDatabaseFilter filter = db.GetFilterFromString(SearchBarText);
                DatabaseItems = db.GetDatabaseItems(filter);
            }
        }
        
        return Task.CompletedTask;
    }
}
