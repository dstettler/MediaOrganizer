using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Runtime;
using System.Threading.Tasks;

namespace MediaOrganizer.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly Window _window;

    [ObservableProperty]
    private string? _dbFilepath;

    public string Greeting => "Welcome to Avalonia!";

    public MainViewModel(Window window)
    {
        _window = window;
    }

    [RelayCommand]
    public async Task HandleDatabaseSelect()
    {
        // Start async operation to open the dialog.
        var file = await _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Database File",
            AllowMultiple = false
        });

        if (file.Count >= 1)
        { 
            // Open reading stream from the first file.
            await using var stream = await file[0].OpenReadAsync();
            
            // Reads all the content of file as a text.
            var fileContent = await streamReader.ReadToEndAsync();
        }
    }
}
