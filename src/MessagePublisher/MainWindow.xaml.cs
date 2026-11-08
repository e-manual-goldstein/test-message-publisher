using System.Windows;
using System.Windows.Controls;

namespace MessagePublisher;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.Password = PasswordBox.Password;
            await vm.InitializeAsync();
        }
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox box && DataContext is MainViewModel vm)
            vm.Password = box.Password;
    }
}
