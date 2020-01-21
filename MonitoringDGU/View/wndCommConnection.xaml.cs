using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO.Ports;
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

namespace MonitoringDGU
{
    /// <summary>
    /// Логика взаимодействия для wndCommConnection.xaml
    /// </summary>
    public partial class wndCommConnection : Window
    {
        public wndCommConnection()
        {
            InitializeComponent();
            this.DataContext = this;
            Ports = new ObservableCollection<string>();
            foreach (string s in SerialPort.GetPortNames())
            {
                Ports.Add(s);
            }
            Speed = 9600;
            StartBits = 1;
            InformationBits = 8;
            StopBits = 1;
        }

        public ObservableCollection<string> Ports
        {
            get;
            set;
        }

        public string CommPort
        {
            get;
            set;
        }


        public int Speed
        {
            get;
            set;
        }

        public int StartBits
        {
            get;
            set;
        }

        public int InformationBits
        {
            get;
            set;
        }

        public int StopBits
        {
            get;
            set;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (CommPort != "" && CommPort != null)
            {
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Поле COM порт не может быть пустым", "Мониторинг ДГУ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

    }
}
