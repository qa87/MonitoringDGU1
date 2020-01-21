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
    /// Логика взаимодействия для wndTO.xaml
    /// </summary>
    public partial class wndTO : Window
    {
        public wndTO()
        {
            InitializeComponent();
            this.DataContext = this;
            Time = 500;
        }

        public int Time
        {
            get;
            set;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {

            this.DialogResult = false;
            this.Close();
        }
    }
}
