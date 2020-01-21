using System;
using System.Collections.ObjectModel;
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
    /// Логика взаимодействия для wndDeviceBase.xaml
    /// </summary>
    public partial class wndDeviceBase : Window
    {
        LocalDataEntities ent = new LocalDataEntities();
        public wndDeviceBase()
        {
            InitializeComponent();
            Devices = new ObservableCollection<Members.Device>();
            this.DataContext = this;
            foreach (var item in ent.Devices)
            {
                Members.Device dev = new Members.Device();
                dev.ID = item.ID;
                dev.IP = item.IP;
                dev.Name = item.Name;
                dev.FullWork = (int)item.FullWork;
                dev.WorkToTO = (int)item.WorkToTO;
                Devices.Add(dev);
            }

        }

        public ObservableCollection<Members.Device> Devices
        {
            get;
            set;
        }

        public Members.Device SelectedDevice
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

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            wndDevice WndDevice = new wndDevice();
            if (WndDevice.ShowDialog() == true)
            {
                Members.Device dev = new Members.Device();
                dev.Name = WndDevice.DevName;
                dev.IP = WndDevice.IP;
                dev.WorkToTO = WndDevice.WorkToTo;
                dev.IsComm = false;
                Devices.Add(dev);
                ent.Devices.Add(new MonitoringDGU.Devices() {Name= dev.Name, IP= dev.IP, FullWork=0, WorkToTO=dev.WorkToTO});
                ent.SaveChanges();

            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedDevice != null)
            {
                

                wndDevice WndDevice = new wndDevice();
                WndDevice.DevName = SelectedDevice.Name;
                WndDevice.IP = SelectedDevice.IP;
                WndDevice.WorkToTo = SelectedDevice.WorkToTO;
                if (WndDevice.ShowDialog() == true)
                {

                    SelectedDevice.Name = WndDevice.DevName;
                    SelectedDevice.IP = WndDevice.IP;
                    SelectedDevice.WorkToTO = WndDevice.WorkToTo;
                    var dev = ent.Devices.FirstOrDefault(d => d.ID == SelectedDevice.ID); //измененя в базе
                    dev.Name = SelectedDevice.Name;
                    dev.IP = SelectedDevice.IP;
                    dev.WorkToTO = SelectedDevice.WorkToTO;
                    ent.SaveChanges();

                }
            }

        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {

            if (SelectedDevice != null)
            {
                if (MessageBox.Show("Действительно удалить запись о ДГУ '" + SelectedDevice.Name + "'? Все связанные данные о наработке и ТО также будут удалены.", "Мониторинг ДГУ", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                   var dev=  ent.Devices.FirstOrDefault(d => d.ID == SelectedDevice.ID);
                   if (dev != null)
                   {
                       ent.Devices.Remove(dev);
                       ent.SaveChanges();
                   }
                   Devices.Remove(SelectedDevice);
                }
            }
        }
    }
}
