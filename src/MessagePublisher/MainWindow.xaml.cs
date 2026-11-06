using System.Windows;
using System.Windows.Controls;

namespace MessagePublisher;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is MainViewModel vm)
                vm.Password = PasswordBox.Password;
        };
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox box && DataContext is MainViewModel vm)
            vm.Password = box.Password;
    }
}
