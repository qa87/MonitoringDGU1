using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MonitoringDGU
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception)
            {
                string message = ((Exception)e.ExceptionObject).Message;
                MessageBox.Show("Необработанная ошибка приложения: " + message, "Мониторинг ДГУ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                MessageBox.Show("Необработанная ошибка приложения. Неудается распознать ошибку." , "Мониторинг ДГУ", MessageBoxButton.OK, MessageBoxImage.Error);
        
            }
        }

    }
}
