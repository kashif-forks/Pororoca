using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Pororoca.Desktop.ViewModels;

namespace Pororoca.Desktop.Views;

public partial class ImportExportCurlWindow : Window
{
    public ImportExportCurlWindow() => AvaloniaXamlLoader.Load(this);

    public void RunOkClicked(object sender, RoutedEventArgs args)
    {
        if (DataContext is ImportExportCurlWindowViewModel vm && vm.RunOkClicked())
        {
            Close();
        }
    }

    public void Cancel(object sender, RoutedEventArgs args) => Close();
}