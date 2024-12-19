using Simscop.Spindisk.Core.NICamera;

namespace PointScan_Test
{
    class ConfigTest
    {
        private string _iniFilepath = System.Environment.CurrentDirectory + "\\config.ini";
        private readonly string _sectionCamera = "Camera";
        private readonly string _sectionSavepath = "File";
        private readonly IniFile _IniFile;

        public ConfigTest()
        {
            //默认设置
            //MinAO,-10;MinAo,10;MinAI,-10;MinAI,10;
            //MinAO,-1;MinAo,1;MinAI,-1;MinAI,1;
            //XPixelFactor,300;YPixelFactor,300
            //PixelDwelTime,20;LowTime,0;
            //DeviceName,Dev1;WaveMode,0

            _IniFile = new IniFile(_iniFilepath);
        }

        private bool WriteData(ConfigEnum config, string data, bool isSavefile = false)
        {
            try
            {
                string section = isSavefile ? _sectionCamera : _sectionSavepath;
                _IniFile.WriteValue(section, config.ToString(), data);
                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private string Read(ConfigEnum config)
        {
            try
            {
                return _IniFile.GetString(_sectionCamera, config.ToString(), string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return string.Empty;
            }

        }

        public double MinAO
        {
            get
            {
                return Double.Parse(Read(ConfigEnum.MinAO));
            }
            set
            {
                WriteData(ConfigEnum.MinAO, value.ToString());
            }
        }
        public double MaxAO
        {
            get
            {
                return Double.Parse(Read(ConfigEnum.MaxAO));
            }
            set
            {
                WriteData(ConfigEnum.MaxAO, value.ToString());
            }
        }
        public double MinAI
        {
            get
            {
                return Double.Parse(Read(ConfigEnum.MinAI));
            }
            set
            {
                WriteData(ConfigEnum.MinAI, value.ToString());
            }
        }
        public double MaxAI
        {
            get
            {
                return Double.Parse(Read(ConfigEnum.MaxAI));
            }
            set
            {
                WriteData(ConfigEnum.MaxAI, value.ToString());
            }
        }
        public double MaxXV
        {
            get
            {
                return Double.Parse(Read(ConfigEnum.MaxXV));
            }
            set
            {
                WriteData(ConfigEnum.MaxXV, value.ToString());
            }
        }
        public double MinXV
        {
            get
            {
                return Double.Parse(Read(ConfigEnum.MinXV));
            }
            set
            {
                WriteData(ConfigEnum.MinXV, value.ToString());
            }
        }
        public double MaxYV
        {
            get
            {
                return Double.Parse(Read(ConfigEnum.MaxYV));
            }
            set
            {
                WriteData(ConfigEnum.MaxYV, value.ToString());
            }
        }
        public double MinYV
        {
            get
            {
                return Double.Parse(Read(ConfigEnum.MinYV));
            }
            set
            {
                WriteData(ConfigEnum.MinYV, value.ToString());
            }
        }
        public int XPixelFactor
        {
            get
            {
                return int.Parse(Read(ConfigEnum.XPixelFactor));
            }
            set
            {
                WriteData(ConfigEnum.XPixelFactor, value.ToString());
            }
        }
        public int YPixelFactor
        {
            get
            {
                return int.Parse(Read(ConfigEnum.YPixelFactor));
            }
            set
            {
                WriteData(ConfigEnum.YPixelFactor, value.ToString());
            }
        }
        public int PixelDwelTime
        {
            get
            {
                return int.Parse(Read(ConfigEnum.PixelDwelTime));
            }
            set
            {
                WriteData(ConfigEnum.PixelDwelTime, value.ToString());
            }
        }
        public int LowTime
        {
            get
            {
                return int.Parse(Read(ConfigEnum.LowTime));
            }
            set
            {
                WriteData(ConfigEnum.LowTime, value.ToString());
            }
        }
        public string DeviceName
        {
            get
            {
                return Read(ConfigEnum.DeviceName);
            }
            set
            {
                WriteData(ConfigEnum.DeviceName, value);
            }
        }
        public int WaveMode
        {
            get
            {
                return int.Parse(Read(ConfigEnum.LowTime));
            }
            set
            {
                WriteData(ConfigEnum.LowTime, value.ToString());
            }
        }

    }
}

