using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringDGU.Members
{
    //Клас контекста главноко окна, объект класса создается в конструкторе главного окна
    public class HeadContext : INotifyPropertyChanged
    {
        /// <summary>
        /// Объявление таймера опроса оборудрования
        /// </summary>
        System.Threading.Timer MyTimer;


        /// <summary>
        /// Таймер для расчета времени наработки
        /// </summary>
        System.Threading.Timer WorkTimer;
        public HeadContext()
        {
            //Создается таймер опроса оборудования
            MyTimer = new System.Threading.Timer(new System.Threading.TimerCallback(GetData), null, Period, Period);
            WorkTimer = new System.Threading.Timer(new System.Threading.TimerCallback(UpdateWorkTime), null, 60000, 60000);
        }

        #region Propertyes

        //Период опроса оборудования, начальное значение 10 сек.
        private int _period = 10;
        public int Period
        {
            get
            {
                return _period;
            }
            set
            {
                _period = value;
                OnPropertyChanged("Period");
            }
        }

        private ObservableCollection<Members.Device> _devices = new ObservableCollection<Device>();

        public ObservableCollection<Members.Device> Devices
        {
            get
            {
                return _devices;
            }
            set
            {
                _devices = value;
            }
        }


        #endregion

        #region Commands

        /// <summary>
        /// Команда подключения через COM порт, вызывается при нажатии кнопки
        /// </summary>
        public Service.BaseCommand CommConnection
        {
            get
            {
                return new Service.BaseCommand(OnCommPortConnection);
            }
        }

        /// <summary>
        /// Команда подключения через TCP к нескоьким устройствам, вызывается при нажатии кнопки
        /// </summary>
        public Service.BaseCommand TcpConnection
        {
            get
            {
                return new Service.BaseCommand(OnTcpConnection);
            }
        }

        #endregion

        #region OnCommands

        private void OnCommPortConnection(object obj)
        {
            try
            {
                wndCommConnection WndComm = new wndCommConnection();
                if (WndComm.ShowDialog() == true)
                {
                    Devices.Clear();
                    Members.Device dev = new Device();
                    dev.IsComm = true;
                    dev.CommPortName = WndComm.CommPort;
                    Devices.Add(dev);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Мониторинг ДГУ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void OnTcpConnection(object obj)
        {
            try
            {
                View.wndDeviceBase WndDeviceBase = new View.wndDeviceBase();
                if (WndDeviceBase.ShowDialog() == true)
                {
                    Devices.Clear();
                    var list = WndDeviceBase.Devices.Where(d => d.IsSelected == true).ToList();
                    foreach (var item in list)
                    {
                        Devices.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Мониторинг ДГУ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        #endregion

        #region Fuctions

        /// <summary>
        /// Функция опроса, вызвается при срабатывании таймера
        /// </summary>
        /// <param name="obj"></param>
        private void GetData(object obj)
        {
            //Останавливаем таймер, на случай если опрашивать будем дольше чем период срабатывания таймера. Если такое произойдет без остановки таймера возможна ошибка при работе с оборудованием. 
            MyTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            try
            {
                foreach (var item in Devices)
                {
                    item.GetData();
                }
            }
            catch (Exception ex)
            {
            }


            //Запускаем таймер заново
            MyTimer.Change(Period * 1000, Period * 1000);
        }


        /// <summary>
        /// Функция обновления наработки
        /// </summary>
        /// <param name="obj"></param>
        private void UpdateWorkTime(object obj)
        {
            try
            {
                foreach (var device in Devices)
                {
                    if (device.IsComm == false && device.IsActive && device.FreqEngine > 0)
                    {
                        device.FullWork++;
                        device.WorkToTO--;
                        //Обновляем наработку и время до то в БД.
                        LocalDataEntities ent = new LocalDataEntities();
                        var devFromDb = ent.Devices.FirstOrDefault(d => d.ID == device.ID);
                        devFromDb.WorkToTO = device.WorkToTO;
                        devFromDb.FullWork = device.FullWork;
                        ent.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }


        #endregion

        #region InotifyPropertyChanged

        /// <summary>
        /// Реализация иметодов для информирования интерфейса об изменении данных
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                try
                {
                    handler(this, new PropertyChangedEventArgs(name));
                }
                catch (Exception ex)
                {
                }
            }
        }

        #endregion

    }
}
