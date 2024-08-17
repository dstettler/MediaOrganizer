using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MediaOrganizer.Models;
using System.ComponentModel;

namespace MediaOrganizer.Views;

public partial class CurrentlySelectedItemControl : UserControl
{
    public static readonly StyledProperty<SqlDatabaseItem> SelectedDatabaseItemProperty =
        AvaloniaProperty.Register<CurrentlySelectedItemControl, SqlDatabaseItem>(nameof(SelectedItem));

    public SqlDatabaseItem SelectedItem
    {
        get
        {
            return GetValue(SelectedDatabaseItemProperty);
        }
    }   

    public CurrentlySelectedItemControl()
    {
        InitializeComponent();
    }
}