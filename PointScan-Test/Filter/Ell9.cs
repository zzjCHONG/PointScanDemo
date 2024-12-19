using System.IO.Ports;

namespace FilterThrolabs
{
    public class ELL9
    {
        private readonly string? _serialNumber = "10900171";
        private string? _portName;
        private string? _portChannel;
        private static int _currentPos;
        private static string? _errMsg;

        private SerialPort _serialPort = new()
        {
            BaudRate = 9600,
            StopBits = StopBits.One,
            DataBits = 8,
            Parity = Parity.None,
        };

        public bool Connect(out string msg)
        {
            msg = string.Empty;
            if (Valid())
            {
                _serialPort.PortName = _portName;
                if (!_serialPort.IsOpen) _serialPort.Open();

                if (_serialPort.IsOpen)
                {
                    msg = "Initialize slider completed!";
                }
                else
                {
                    msg = $"Failed to connect to port {_portName}";
                }
                return _serialPort.IsOpen;
            }
            msg = "No available slider serial port found";
            return false;
        }

        public bool Disconnect()
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
                _serialPort.Dispose();
            }
            return _serialPort.IsOpen == false;
        }

        public bool BackWard()
        {
            string data = $"{_portChannel}bw";
            return Send(data);
        }

        public bool ForWard()
        {
            string data = $"{_portChannel}fw";
            return Send(data);
        }

        public bool Home(bool isLeftSide)
        {
            int code = isLeftSide ? 1 : 0;
            string data = $"{_portChannel}ho{code}";
            return Send(data);
        }

        public bool Update()
        {
            string data = $"{_portChannel}gp";
            return Send(data);
        }

        public bool MovetoPos(int index)
        {
            string value = string.Empty;
            switch (index)
            {
                case 1:
                    value = "00";
                    break;
                case 2:
                    value = "20";
                    break;
                case 3:
                    value = "40";
                    break;
                case 4:
                    value = "60";
                    break;
                default:
                    return false;
            }
            string data = "Tx: 0ma000000" + value;
            return Send(data);
        }

        private bool Send(string data)
        {
            if (!_serialPort.IsOpen) return false;
            if (!Write(data)) return false;
            Thread.Sleep(200);
            if (!CheckMoveRtn(_serialPort.ReadExisting())) return false;
            return true;
        }

        private bool CheckMoveRtn(string rtn)
        {
            _errMsg = string.Empty;
            try
            {
                if (rtn.Substring(0, 3) == $"{_portChannel}PO")
                {
                    string str = rtn.Substring(3, rtn.Length - 5);//0000001F
                    _currentPos = Convert.ToInt32(str, 16);
                    Console.WriteLine($"position_{_currentPos}");
                    return true;
                }
                else
                {
                    if (rtn.Substring(0, 3) == $"{_portChannel}GS")
                    {
                        int code = Convert.ToInt32(rtn.Substring(3, 2), 16);
                        _errMsg = ((GSErrorEnum)code).ToString();
                        Console.WriteLine("GS:" + (GSErrorEnum)code);
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

        }

        private bool Write(string data)
        {
            if (!_serialPort.IsOpen)
                return false;

            _serialPort.Write(data);
            return true;
        }

        private bool CheckPort(string portName)
        {
            SerialPort port = new SerialPort(portName);
            try
            {
                port.Open();
                Console.WriteLine($"串口 {portName} 未被占用");
                if (port.IsOpen) port.Close();
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"串口 {portName} 已被占用");

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"打开串口 {portName} 发生错误: {ex.Message}");
                return true;
            }
        }

        private bool Valid()
        {
            try
            {
                string[] portNames = SerialPort.GetPortNames();
                foreach (string portName in portNames)
                {
                    if (!CheckPort(portName)) continue;

                    if (!_serialPort.IsOpen)
                    {
                        _serialPort.PortName = portName;
                        _serialPort.Open();
                    }
                    _serialPort.Write($"0in");
                    Thread.Sleep(300);
                    if (CheckRead(_serialPort.ReadExisting()))
                    {
                        _portChannel = "0";//有多个pin查找，此处省略
                        _portName = portName;
                        _serialPort.Close();
                        break;
                    }
                    _serialPort.Close();
                }
                return !string.IsNullOrEmpty(_portName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        private bool CheckRead(string str)
        {
            //0 IN 09 10900171 2023 10 01 005D 00000000

            //0 IN 06 10600345 2023 12 01 001F 00000000  --
            //0 IN 06 10600320 2023 12 01 001F 00000000  --
            try
            {
                if (string.IsNullOrEmpty(str)) return false;
                string serialNumber = str.Remove(str.Length - 4, 4).Substring(5, 8);
                if (serialNumber != _serialNumber) return false;//序列号验证
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public enum GSErrorEnum
        {
            NoError,
            CommunicationTimeOut,
            MechanicalTimeOut,
            CommandErrorOrNotSupported,
            ValueOutofRange,
            ModuleIsolated,
            ModuleOutofIsolation,
            InitializingError,
            ThermalError,
            Busy,
            SensorError,
            MotorError,
            OutofRange,
            OverCurrentError,
            Reserved,
        }

        ~ELL9() => Disconnect();
    }
}
