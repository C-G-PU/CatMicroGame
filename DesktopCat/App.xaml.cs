using System.Configuration;
using System.Data;
using System.Windows;
using System.IO;
using System;

namespace DesktopCat;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    public App()
    {
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        File.WriteAllText("crash_log.txt", "Dispatcher Error: " + e.Exception.ToString());
        System.Windows.MessageBox.Show("Произошла ошибка: " + e.Exception.Message, "Ошибка DesktopCat", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true; // prevent crash if possible
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            File.WriteAllText("crash_domain_log.txt", "Domain Error: " + ex.ToString());
            System.Windows.MessageBox.Show("Критическая ошибка: " + ex.Message, "Ошибка DesktopCat", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
