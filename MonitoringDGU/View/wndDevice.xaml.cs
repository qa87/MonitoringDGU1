using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MonitoringDGU.View
{
    /// <summary>
    /// Логика взаимодействия для wndDevice.xaml
    /// </summary>
    public partial class wndDevice : Window
    {
        public wndDevice()
        {
            InitializeComponent();
            this.DataContext = this;
            this.WorkToTo = 500;
        }

        public string DevName
        {
            get;
            set;
        }

        public string IP
        {
            get;
            set;
        }

        public int WorkToTo
        {
            get;
            set;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
