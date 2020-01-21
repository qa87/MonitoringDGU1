using System;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Net.Sockets;
using System.Net;

namespace MonitoringDGU.Members
{
    //Класс экземпляра оборудования
    public class Device : INotifyPropertyChanged
    {
        List<string> ReciveLog = new List<string>();
        /// <summary>
        /// Порт према данных по TCP
        /// </summary>
        private static int PortListener = 502;
        /// <summary>
        /// Порт передачи данных на TCP
        /// </summary>
        private const int PortWriter = 502;
        //Коэфициенты, на которые делится полученное значение
        private const int FreqCoof = 10;
        private const int PowerCoof = 256;
        private const double EngineSpeedCoof = 1;
        private const double OilPreasureCoof = 10;
        private const int BattaryVoltageCoof = 10;
        /// <summary>
        /// Объявление таймера ожидания ответа
        /// </summary>
        Timer ResponseTimer;

        //Объявление ком порта
        SerialPort sp;

        //Сокет приема  и отправки данных по сети
        TcpClient ClientTCP;

        NetworkStream tcpStream;
        /// <summary>
        /// Буфер для приема данных по сети
        /// </summary>
        byte[] TCP_buffer = new byte[1024];
        //Флаг ожидания ответа
        bool IsWaitResponse = false;
        //Тип последнего отправленного запроса
        QueryType LastQueryType;
        //Запрос регистров 3-6
        byte[] LineParams = { 0x01, 0x03, 0x00, 0x03, 0x00, 0x06, 0x35, 0xC8 };
        //Запрос регистров 16-17
        byte[] GetActivePower = { 0x01, 0x03, 0x00, 0x16, 0x00, 0x02, 0x25, 0xCF };
        //Запрос регистра 13
        byte[] GetFreq = { 0x01, 0x03, 0x00, 0x13, 0x00, 0x01, 0x75, 0xCF };
        //Запрос регистра 3D
        byte[] GetMode = { 0x01, 0x03, 0x00, 0x3D, 0x00, 0x01, 0x15, 0xC6 };
        //Запрос регистров 2A-2F
        byte[] GetEngineParams = { 0x01, 0x03, 0x00, 0x2A, 0x00, 0x06, 0xE4, 0x00 };
        //Команда запроса ошибок и предупреждений
        byte[] GetWarningAndErrors = { 0x01, 0x03, 0x00, 0x32, 0x00, 0x06, 0x64, 0x07 };


        #region Propertyes

        /// <summary>
        /// Флаг указывающий на подключение через COM порт.
        /// </summary>
        public bool IsComm
        {
            get;
            set;
        }

        //Уникальный идентификатор формируется в БД
        public int ID
        {
            get;
            set;
        }

        /// <summary>
        /// Название
        /// </summary>
        private string _name = "";
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                OnPropertyChanged("Name");
                OnPropertyChanged("StringName");
            }
        }

        /// <summary>
        /// IP адрес
        /// </summary>
        private string _ip = "";

        public string IP
        {
            get
            {
                return _ip;
            }
            set
            {
                _ip = value;
                OnPropertyChanged("IP");
                OnPropertyChanged("StringName");
            }
        }

        /// <summary>
        /// Имя ком порта при соединении через ком порт
        /// </summary>
        public string CommPortName
        {
            get;
            set;
        }

        /// <summary>
        /// Полная наработка в минутах
        /// </summary>
        private int _fullWork = 0;
        public int FullWork
        {
            get
            {
                return _fullWork;
            }
            set
            {
                _fullWork = value;
                OnPropertyChanged("FullWorkString");
                OnPropertyChanged("FullWork");
            }
        }

        /// <summary>
        /// Время до ТО в минутах
        /// </summary>
        private int _workToTo = 0;
        public int WorkToTO
        {
            get
            {
                return _workToTo;
            }
            set
            {
                _workToTo = value;
                OnPropertyChanged("WorkToTO");
                OnPropertyChanged("WorkToTOString");
                OnPropertyChanged("IsTO");
            }
        }

        //
        //Флаг выбора в окне "База ДГУ"
        private bool _isSelected = true;
        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                _isSelected = value;
                OnPropertyChanged("IsSelected");
            }
        }

        /// <summary>
        /// Флаг активности ДГУ, отвечает или нет
        /// </summary>
        private bool _isActive = false;
        public bool IsActive
        {
            get
            {
                return _isActive;
            }
            set
            {
                _isActive = value;
                OnPropertyChanged("ActiveString");
            }
        }

        public string ActiveString
        {
            get
            {
                if (IsActive)
                {
                    return "/MonitoringDGU;component/Images/OnConnection.png";
                }
                return "/MonitoringDGU;component/Images/NoConnection.png";
            }
        }

        /// <summary>
        /// Формирование имени для оторажения
        /// </summary>
        public string StringName
        {
            get
            {
                if (IsComm)
                {
                    return CommPortName;
                }
                return Name + " IP: " + IP;
            }
        }

        /// <summary>
        /// Время последнего полученного ответа
        /// </summary>
        private DateTime _lastResultDT;
        public DateTime LastResultDT
        {
            get
            {
                return _lastResultDT;
            }
            set
            {
                _lastResultDT = value;
                OnPropertyChanged("LastResultDT");
            }
        }

        /// <summary>
        /// Формирования текстового представления времени наработки
        /// </summary>
        public string FullWorkString
        {
            get
            {
                if (IsComm)
                    return "-";
                int minutes = 0;
                int hours = Math.DivRem(FullWork, 60, out minutes);
                return hours + ":" + minutes;
            }
        }

        /// <summary>
        /// Формирования текстового представления времени до ТО
        /// </summary>
        public string WorkToTOString
        {
            get
            {
                if (IsComm)
                    return "-";
                int minutes = 0;
                int hours = Math.DivRem(WorkToTO, 60, out minutes);
                return hours + ":" + minutes;
            }
        }

        /// <summary>
        /// Флаг отображения кнопки
        /// </summary>
        public bool IsTO
        {
            get
            {
                if (IsComm)
                    return false;
                if (WorkToTO <= 0)
                    return true;
                return false;
            }
        }


        //Флаг ручного режима
        private bool _isManual = false;
        public bool IsManual
        {
            get
            {
                return _isManual;
            }
            set
            {
                _isManual = value;
                OnPropertyChanged("ManualString");
            }
        }

        public string ManualString
        {
            get
            {
                if (IsManual)
                {
                    return "/MonitoringDGU;component/Images/OnConnection.png";
                }
                return "/MonitoringDGU;component/Images/NoConnection.png";
            }
        }

        //Флаг автоматического режима
        private bool _isAuto = false;
        public bool IsAuto
        {
            get
            {
                return _isAuto;
            }
            set
            {
                _isAuto = value;
                OnPropertyChanged("AutoString");
            }
        }

        public string AutoString
        {
            get
            {
                if (IsAuto)
                {
                    return "/MonitoringDGU;component/Images/OnConnection.png";
                }
                return "/MonitoringDGU;component/Images/NoConnection.png";
            }
        }

        //Флаг режима 'Отключен'
        private bool _isOff = false;
        public bool IsOff
        {
            get
            {
                return _isOff;
            }
            set
            {
                _isOff = value;
                OnPropertyChanged("OffString");
            }
        }

        public string OffString
        {
            get
            {
                if (IsOff)
                {
                    return "/MonitoringDGU;component/Images/OnConnection.png";
                }
                return "/MonitoringDGU;component/Images/NoConnection.png";
            }
        }

        /// <summary>
        /// Флаг режима тестирования
        /// </summary>
        private bool _isTest = false;
        public bool IsTest
        {
            get
            {
                return _isTest;
            }
            set
            {
                _isTest = value;
                OnPropertyChanged("TestString");
            }
        }

        public string TestString
        {
            get
            {
                if (IsTest)
                {
                    return "/MonitoringDGU;component/Images/OnConnection.png";
                }
                return "/MonitoringDGU;component/Images/NoConnection.png";
            }
        }

        /// <summary>
        /// Флаг режима тестирования под нагрузкой
        /// </summary>
        private bool _isLoadTest = false;
        public bool IsLoadTest
        {
            get
            {
                return _isLoadTest;
            }
            set
            {
                _isLoadTest = value;
                OnPropertyChanged("LoadTestString");
            }
        }

        public string LoadTestString
        {
            get
            {
                if (IsLoadTest)
                {
                    return "/MonitoringDGU;component/Images/OnConnection.png";
                }
                return "/MonitoringDGU;component/Images/NoConnection.png";
            }
        }


        //Флаг автоматического режима
        private bool _isErrorExist = false;
        public bool IsErrorExist
        {
            get
            {
                return _isErrorExist;
            }
            set
            {
                _isErrorExist = value;
                OnPropertyChanged("IsErrorExist");
                OnPropertyChanged("IsErrorExistString");
            }
        }

        public string IsErrorExistString
        {
            get
            {
                if (!IsErrorExist)
                {
                    return "/MonitoringDGU;component/Images/OnConnection.png";
                }
                return "/MonitoringDGU;component/Images/error.png";
            }
        }





        /// <summary>
        /// Напряжение фаза 1
        /// </summary>
        private double _VoltageF1 = 0;
        public double VoltageF1
        {
            get
            {
                return _VoltageF1;
            }
            set
            {
                _VoltageF1 = value;
                OnPropertyChanged("VoltageF1");
            }
        }

        /// <summary>
        /// Напряжение фаза 2
        /// </summary>
        private double _VoltageF2 = 0;
        public double VoltageF2
        {
            get
            {
                return _VoltageF2;
            }
            set
            {
                _VoltageF2 = value;
                OnPropertyChanged("VoltageF2");
            }
        }

        /// <summary>
        /// Напряжение фаза 3
        /// </summary>
        private double _VoltageF3 = 0;
        public double VoltageF3
        {
            get
            {
                return _VoltageF3;
            }
            set
            {
                _VoltageF3 = value;
                OnPropertyChanged("VoltageF3");
            }
        }

        /// <summary>
        /// Ток фаза 1
        /// </summary>
        private double _CurrentF1 = 0;
        public double CurrentF1
        {
            get
            {
                return _CurrentF1;
            }
            set
            {
                _CurrentF1 = value;
                OnPropertyChanged("CurrentF1");
            }
        }

        /// <summary>
        /// Ток фаза 2
        /// </summary>
        private double _CurrentF2 = 0;
        public double CurrentF2
        {
            get
            {
                return _CurrentF2;
            }
            set
            {
                _CurrentF2 = value;
                OnPropertyChanged("CurrentF2");
            }
        }

        /// <summary>
        /// Ток фаза 3
        /// </summary>
        private double _CurrentF3 = 0;
        public double CurrentF3
        {
            get
            {
                return _CurrentF3;
            }
            set
            {
                _CurrentF3 = value;
                OnPropertyChanged("CurrentF3");
            }
        }


        /// <summary>
        /// Частота
        /// </summary>
        private double _freq = 0;
        public double Freq
        {
            get
            {
                return _freq;
            }
            set
            {
                _freq = value;
                OnPropertyChanged("Freq");
            }
        }

        /// <summary>
        /// Активная мощность
        /// </summary>
        private double _power = 0;
        public double Power
        {
            get
            {
                return _power;
            }
            set
            {
                _power = value;
                OnPropertyChanged("Power");
            }
        }

        /// <summary>
        /// Частота вращения двигателя
        /// </summary>
        private double _freqEngine = 0;
        public double FreqEngine
        {
            get
            {
                return _freqEngine;
            }
            set
            {
                _freqEngine = value;
                OnPropertyChanged("FreqEngine");
            }
        }


        /// <summary>
        /// Давление маcла
        /// </summary>
        private double _oilPressure = 0;
        public double OilPressure
        {
            get
            {
                return _oilPressure;
            }
            set
            {
                _oilPressure = value;
                OnPropertyChanged("OilPressure");
            }
        }


        /// <summary>
        /// Температура
        /// </summary>
        private double _coolantTemp = 0;
        public double CoolantTemp
        {
            get
            {
                return _coolantTemp;
            }
            set
            {
                _coolantTemp = value;
                OnPropertyChanged("CoolantTemp");
            }
        }


        /// <summary>
        /// Уровень топлива
        /// </summary>
        private double _fuelLevel = 0;
        public double FuelLevel
        {
            get
            {
                return _fuelLevel;
            }
            set
            {
                _fuelLevel = value;
                OnPropertyChanged("FuelLevel");
            }
        }


        /// <summary>
        /// Напряжение на батарее
        /// </summary>
        private double _battaryVoltage = 0;
        public double BattaryVoltage
        {
            get
            {
                return _battaryVoltage;
            }
            set
            {
                _battaryVoltage = value;
                OnPropertyChanged("BattaryVoltage");
            }
        }


        #endregion

        #region Commands

        public Service.BaseCommand TO
        {
            get
            {
                return new Service.BaseCommand(OnTO);
            }
        }

        #endregion

        #region Function

        private void OnTO(object obj)
        {
            View.wndTO WndTo = new View.wndTO();
            if (WndTo.ShowDialog() == true)
            {
                WorkToTO = WndTo.Time * 60;
            }


        }

        //Функция опроса
        public void GetData()
        {
            string analyseString = "";
            try
            {
                if (IsComm)
                {
                    //Создаем ком порт, подписываемся на событие получение данных, и отправляем поочередно запросы после каждой отправки ждем ответ
                    sp = new SerialPort(CommPortName, 9600, Parity.None, 8, StopBits.One);


                    sp.DataReceived += sp_DataReceived;
                    sp.Open();

                    //Запрос режима работы
                    IsWaitResponse = true;
                    LastQueryType = QueryType.Mode;
                    sp.Write(GetMode, 0, GetMode.Length);
                    ResponseWaiting();


                    //Запрос параметров двигателя
                    IsWaitResponse = true;
                    LastQueryType = QueryType.EngineParams;
                    sp.Write(GetEngineParams, 0, GetEngineParams.Length);
                    ResponseWaiting();

                    ///Запрос параметров напряжения и тока по всем фазам
                    IsWaitResponse = true;
                    LastQueryType = QueryType.LineParams;
                    sp.Write(LineParams, 0, LineParams.Length);
                    ResponseWaiting();

                    //Запрос значения активной мощности
                    IsWaitResponse = true;
                    LastQueryType = QueryType.ActivPower;
                    sp.Write(GetActivePower, 0, GetActivePower.Length);
                    ResponseWaiting();

                    //Запрос значения частоты
                    IsWaitResponse = true;
                    LastQueryType = QueryType.Freq;
                    sp.Write(GetFreq, 0, GetFreq.Length);
                    ResponseWaiting();

                    //Запрос ошибок
                    IsWaitResponse = true;
                    LastQueryType = QueryType.Errors;
                    sp.Write(GetWarningAndErrors, 0, GetWarningAndErrors.Length);
                    ResponseWaiting();



                    sp.Close();

                }
                else
                {
                    analyseString += "Получение локального адреса: ";
                    var ipHostInfo = Dns.GetHostByName(Dns.GetHostName()).AddressList;
                    foreach (var item in ipHostInfo)
                        analyseString += item.ToString();
                    analyseString += "  0: " +ipHostInfo[0];
                    IPAddress ipAddress = ipHostInfo[0];
                    IPEndPoint localEndPoint = new IPEndPoint(ipAddress, PortListener);
                    PortListener++;
                    analyseString += "Получение адреса ДГУ: " + IP;
                    IPAddress serverAdress = IPAddress.Parse(IP);
                    IPEndPoint serverEndPoint = new IPEndPoint(serverAdress, PortWriter);

                    ///Создаем объект TcpClient с конфигурацией локальной точки
                    if (ClientTCP == null)
                    {
                        ClientTCP = new TcpClient(localEndPoint);
                        //Подключаемся к удаленной точки
                        ClientTCP.Connect(serverEndPoint);
                        tcpStream = ClientTCP.GetStream();
                    }
                    ///Получаем поток
                    
                    


                        /* LastQueryType = QueryType.EngineParams;
                         tcpStream.Write(GetEngineParams, 0, GetEngineParams.Length);
                         Thread.Sleep(50);
                         tcpStream.Read(TCP_buffer, 0, 512);
                         TCPRecive(null);

                         LastQueryType = QueryType.Mode;
                         tcpStream.Write(GetMode, 0, GetMode.Length);
                         Thread.Sleep(50);
                         tcpStream.Read(TCP_buffer, 0, 512);
                         TCPRecive(null);*/


                        System.AsyncCallback MyCallBack = new System.AsyncCallback(TCPRecive);

                        //Connect
                        /* IsWaitResponse = true;
                         LastQueryType = QueryType.TCP_Connect;
                         tcpStream.BeginRead(TCP_buffer, 0, 1024, MyCallBack, null);
                         tcpStream.Write(GetMode, 0, GetMode.Length);
                         ResponseWaiting();*/


                        //Запрос режима работы
                        IsWaitResponse = true;
                        LastQueryType = QueryType.Mode;
                       // tcpStream.BeginRead(TCP_buffer, 0, 1024, MyCallBack, null);
                        tcpStream.Write(GetMode, 0, GetMode.Length);
                        Thread.Sleep(150);
                        tcpStream.Read(TCP_buffer, 0, 64);
                        TCPRecive(null);


                       // ResponseWaiting();

                        //Запрос параметров двигателя
                        IsWaitResponse = true;
                        LastQueryType = QueryType.EngineParams;
                       // tcpStream.BeginRead(TCP_buffer, 0, 1024, MyCallBack, null);
                        tcpStream.Write(GetEngineParams, 0, GetEngineParams.Length);
                       // ResponseWaiting();
                        Thread.Sleep(150);
                        tcpStream.Read(TCP_buffer, 0, 64);
                        TCPRecive(null);

                        ///Запрос параметров напряжения и тока по всем фазам
                        IsWaitResponse = true;
                         LastQueryType = QueryType.LineParams;
                       //  tcpStream.BeginRead(TCP_buffer, 0, 1024, MyCallBack, null);
                         tcpStream.Write(LineParams, 0, LineParams.Length);
                    // ResponseWaiting();
                    Thread.Sleep(150);
                    tcpStream.Read(TCP_buffer, 0, 64);
                    TCPRecive(null);

                    //Запрос значения активной мощности
                       IsWaitResponse = true;
                       LastQueryType = QueryType.ActivPower;
                     //  tcpStream.BeginRead(TCP_buffer, 0, 1024, MyCallBack, null);
                       tcpStream.Write(GetActivePower, 0, GetActivePower.Length);
                    //ResponseWaiting();
                    Thread.Sleep(150);
                    tcpStream.Read(TCP_buffer, 0, 64);
                    TCPRecive(null);

                    //Запрос значения частоты
                     IsWaitResponse = true;
                     LastQueryType = QueryType.Freq;
                    // tcpStream.BeginRead(TCP_buffer, 0, 1024, MyCallBack, null);
                     tcpStream.Write(GetFreq, 0, GetFreq.Length);
                    //ResponseWaiting();
                    Thread.Sleep(150);
                    tcpStream.Read(TCP_buffer, 0, 64);
                    TCPRecive(null);


                    //Запрос ошибок
                      IsWaitResponse = true;
                      LastQueryType = QueryType.Errors;
                    // tcpStream.BeginRead(TCP_buffer, 0, 1024, MyCallBack, null);
                    tcpStream.Write(GetWarningAndErrors, 0, GetWarningAndErrors.Length);
                    //     ResponseWaiting();

                    Thread.Sleep(150);
                    tcpStream.Read(TCP_buffer, 0, 64);
                    TCPRecive(null);


                    // ClientTCP.Close();

                }
            }
            catch (Exception ex)
            {
              //  System.Windows.MessageBox.Show(analyseString+ex.Message); //добавлено для тестирования
                IsActive = false;
                tcpStream.Close();
                ClientTCP.Close();
                
                IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, PortListener);
                PortListener++;
                IPAddress serverAdress = IPAddress.Parse(IP);
                IPEndPoint serverEndPoint = new IPEndPoint(serverAdress, PortWriter);
                ClientTCP = new TcpClient(localEndPoint);
                //Подключаемся к удаленной точки
                ClientTCP.Connect(serverEndPoint);
                tcpStream = ClientTCP.GetStream();

            }
            finally
            {
               /* if (ClientTCP != null)
                    if(ClientTCP.Connected)
                    ClientTCP.Close();*/
            }
        }

        //Функция вызывается при отсутствии ответа
        public void NoResponse(object obj)
        {
            IsActive = false;
            IsWaitResponse = false;
        }

        private void ResponseWaiting()
        {
            if (ResponseTimer == null)
                ResponseTimer = new Timer(new TimerCallback(NoResponse), null, 500, 500);
            else
                ResponseTimer.Change(500, 500);

            while (IsWaitResponse)
            {
                Thread.Sleep(25);
            }
            ResponseTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void TCPRecive(IAsyncResult result)
        {
           // Thread.Sleep(300);
           /* string reciveString = ByteArrayToString(TCP_buffer);
            reciveString.ToString();
            ReciveLog.Add(reciveString);*/
            
            
            if (TCP_buffer[0] == 0x01 && TCP_buffer[1] == 0x03)
            {
                switch (LastQueryType)
                {
                    case QueryType.ActivPower:

                        byte[] Value1buffer = new byte[9];
                        Array.Copy(TCP_buffer, 0, Value1buffer, 0, 9);
                        ParseByteArray(Value1buffer);
                        break;
                    case QueryType.EngineParams:
                        byte[] Value2buffer = new byte[17];
                        Array.Copy(TCP_buffer, 0, Value2buffer, 0, 17);
                        ParseByteArray(Value2buffer);
                        break;
                    case QueryType.Freq:
                        byte[] Value3buffer = new byte[7];
                        Array.Copy(TCP_buffer, 0, Value3buffer, 0, 7);
                        ParseByteArray(Value3buffer);
                        break;
                    case QueryType.LineParams:
                        byte[] Value4buffer = new byte[17];
                        Array.Copy(TCP_buffer, 0, Value4buffer, 0, 17);
                        ParseByteArray(Value4buffer);
                        break;
                    case QueryType.Mode:

                        byte[] Value5buffer = new byte[7];
                        Array.Copy(TCP_buffer, 0, Value5buffer, 0, 7);
                        ParseByteArray(Value5buffer);
                        break;
                    case QueryType.Errors:
                         byte[] Value6buffer = new byte[17];
                        Array.Copy(TCP_buffer, 0, Value6buffer, 0, 17);
                        ParseByteArray(Value6buffer);
                        break;
                }
            }
        }

        void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                Thread.Sleep(200);
                int bytes = sp.BytesToRead;
                byte[] buffer = new byte[bytes];
                sp.Read(buffer, 0, bytes);
                ParseByteArray(buffer);

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Мониторинг ДГУ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void ParseByteArray(byte[] buffer)
        {
            IsActive = true;
            string reciveString = ByteArrayToString(buffer);
            reciveString.ToString();
            switch (LastQueryType)
            {
                case QueryType.ActivPower:
                    if (buffer.Length == 9)
                    {
                        byte[] powerValue = new byte[4];
                        powerValue[0] = buffer[6];
                        powerValue[1] = buffer[5];
                        powerValue[2] = buffer[4];
                        powerValue[3] = buffer[3];
                        Power = getIntFromBitArray(new BitArray(powerValue)) / PowerCoof;
                    }
                    break;
                case QueryType.EngineParams:
                    if (buffer.Length == 17)
                    {

                        byte[] byteEngineSpeed = new byte[2];
                        byteEngineSpeed[0] = buffer[4];
                        byteEngineSpeed[1] = buffer[3];
                        byte[] byteOilPreasure = new byte[2];
                        byteOilPreasure[0] = buffer[6];
                        byteOilPreasure[1] = buffer[5];
                        byte[] byteTemp = new byte[2];
                        byteTemp[0] = buffer[8];
                        byteTemp[1] = buffer[7];
                        byte[] byteFuelLevel = new byte[2];
                        byteFuelLevel[0] = buffer[10];
                        byteFuelLevel[1] = buffer[9];
                        byte[] byteBattaryVoltage = new byte[2];
                        byteBattaryVoltage[0] = buffer[14];
                        byteBattaryVoltage[1] = buffer[13];

                        FreqEngine = getIntFromBitArray(new BitArray(byteEngineSpeed)) / EngineSpeedCoof;
                        OilPressure = getIntFromBitArray(new BitArray(byteOilPreasure)) / OilPreasureCoof;
                        CoolantTemp = getIntFromBitArray(new BitArray(byteTemp));
                        FuelLevel = getIntFromBitArray(new BitArray(byteFuelLevel));
                        BattaryVoltage = getIntFromBitArray(new BitArray(byteBattaryVoltage)) / BattaryVoltageCoof;

                    }
                    break;
                case QueryType.Freq:
                    if (buffer.Length == 7)
                    {

                        byte[] freqValue = new byte[4];
                        freqValue[0] = buffer[4];
                        freqValue[1] = buffer[3];
                        Freq = getIntFromBitArray(new BitArray(freqValue)) / FreqCoof;
                    }
                    break;
                case QueryType.LineParams:
                    if (buffer.Length == 17)
                    {

                        byte[] byteVoltage_1 = new byte[2];
                        byteVoltage_1[0] = buffer[4];
                        byteVoltage_1[1] = buffer[3];
                        byte[] byteVoltage_2 = new byte[2];
                        byteVoltage_2[0] = buffer[6];
                        byteVoltage_2[1] = buffer[5];
                        byte[] byteVoltage_3 = new byte[2];
                        byteVoltage_3[0] = buffer[8];
                        byteVoltage_3[1] = buffer[7];
                        byte[] byteCurrent_1 = new byte[2];
                        byteCurrent_1[0] = buffer[10];
                        byteCurrent_1[1] = buffer[9];
                        byte[] byteCurrent_2 = new byte[2];
                        byteCurrent_2[0] = buffer[12];
                        byteCurrent_2[1] = buffer[11];
                        byte[] byteCurrent_3 = new byte[2];
                        byteCurrent_3[0] = buffer[14];
                        byteCurrent_3[1] = buffer[13];
                        VoltageF1 = getIntFromBitArray(new BitArray(byteVoltage_1));
                        VoltageF2 = getIntFromBitArray(new BitArray(byteVoltage_2));
                        VoltageF3 = getIntFromBitArray(new BitArray(byteVoltage_3));
                        CurrentF1 = getIntFromBitArray(new BitArray(byteCurrent_1));
                        CurrentF2 = getIntFromBitArray(new BitArray(byteCurrent_2));
                        CurrentF3 = getIntFromBitArray(new BitArray(byteCurrent_3));
                    }
                    break;
                case QueryType.Mode:
                    if (buffer.Length == 7)
                    {
                        byte[] value = new byte[1];
                        value[0] = buffer[4];
                        BitArray ModeBitValue = new BitArray(value);
                        IsManual = ModeBitValue[3];
                        IsAuto = ModeBitValue[4];
                        IsOff = ModeBitValue[5];
                        IsTest = ModeBitValue[6];
                        IsLoadTest = ModeBitValue[7];
                    }
                    break;
                case QueryType.Errors:
                    if (buffer.Length == 17)
                    {
                        byte value=0x00;

                        for (int n = 0; n < 12; n++)
                        {
                            if (buffer[3+n] !=0x00)
                            {
                                value = buffer[3 + n];
                            }
                            
                        }

                        if (value == 0x00)
                            IsErrorExist = false;
                        else
                            IsErrorExist = true;

                    }
                    break;
            }
            LastResultDT = DateTime.Now;
            IsWaitResponse = false;
        }

        private int getIntFromBitArray(BitArray bitArray)
        {
            int value = 0;

            for (int i = 0; i < bitArray.Count; i++)
            {
                if (bitArray[i])
                    value += Convert.ToInt32(Math.Pow(2, i));
            }

            return value;
        }

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
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
