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

namespace MediaOrganizer.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly Window _window;

    /// <summary>
    /// The SQL database
    /// </summary>
    [ObservableProperty]
    private SqlDatabase? _sqlDatabase;

    public string Greeting => "Welcome to Avalonia!";

    public MainViewModel(Window window)
    {
        _window = window;
    }

    [RelayCommand]
    public async Task HandleOrganizerCreate()
    {
        var file = await _window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { Title = "New Organizer" });
        if (file is not null)
        {
            SqlDatabase = new SqlDatabase(file.Name);
            SqlDatabase.CreateSqliteDatabase();

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
        SqlDatabase.GetDatabaseItems(filter);
        return Task.CompletedTask;
    }
}
