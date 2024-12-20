using NationalInstruments;
using NationalInstruments.DAQmx;
using OpenCvSharp;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Markup;
using System.Windows.Media.Media3D;

namespace PointScan_Test
{
    //todo,中途停止部分，会导致无法正常采集，待修复

    public class NIControl
    {
        #region wave-setting

        /// <summary>
        /// 设备名称
        /// </summary>
        public string DeviceName { get; set; } = "Dev1";
        /// <summary>
        /// 波形模式
        /// 0-锯齿波，1-三角波
        /// </summary>
        public int WaveModel { get; set; } = 0;
        /// <summary>
        /// 原始X位置
        /// </summary>
        public int OriginX { get; set; } = 300;
        /// <summary>
        /// 原始Y位置
        /// </summary>
        public int OriginY { get; set; } = 300;
        /// <summary>
        /// X偏移
        /// 锯齿波-偏移取后半部分，避开错位部分
        /// 三角波-偏移足够多行，供偶数行调整位置与奇数行重合
        /// </summary>
        public int XMargin { get; set; } = 15;
        /// <summary>
        /// Y偏移
        /// </summary>
        public int YMargin { get; } = 0;
        /// <summary>
        /// X方向像素数量
        /// </summary>
        public int XPts { get; set; }
        /// <summary>
        /// Y方向像素数量
        /// </summary>
        public int YPts { get; set; }
        /// <summary>
        /// X轴最大电压
        /// </summary>
        public double Max_x_v { get; set; } = 1;
        /// <summary>
        /// X轴最小电压
        /// </summary>
        public double Min_x_v { get; set; } = -1;
        /// <summary>
        /// Y轴最大电压
        /// </summary>
        public double Max_y_v { get; set; } = 1;
        /// <summary>
        /// Y轴最小电压
        /// </summary>
        public double Min_y_v { get; set; } = -1;
        /// <summary>
        /// 最小输出电压
        /// </summary>
        public double MinAO { get; set; } = -10;
        /// <summary>
        /// 最大输出电压
        /// </summary>
        public double MaxAO { get; set; } = 10;
        /// <summary>
        /// 最小输入电压
        /// </summary>
        public double MinAI { get; set; } = -10;
        /// <summary>
        /// 最大输入电压
        /// </summary>
        public double MaxAI { get; set; } = 10;
        /// <summary>
        /// 低电平时间
        /// </summary>
        public double LowTime { get; set; } = 0;
        /// <summary>
        /// 像素停留时间
        /// </summary>
        public double PixelDwelTime { get; set; } = 20;
        /// <summary>
        /// 三角波偏移
        /// </summary>
        public int XOffsetforTriangle { get; set; } = 50;

        /// <summary>
        /// 采样速率(Hz)
        /// </summary>
        public double _rate;
        /// <summary>
        /// 每通道的采样点数
        /// </summary>
        public int _sampsPerChan;
        /// <summary>
        /// 波形输出
        /// </summary>
        public double[]? _waveformArray;

        public void SetParamandGenWaveform()
        {
            if (Convert.ToBoolean(WaveModel))
            {
                XMargin = XOffsetforTriangle;
                Debug.WriteLine(XMargin);
            }
                
            //设置参数
            XPts = OriginX + XMargin;
            YPts = OriginY + YMargin;
            _sampsPerChan = XPts * YPts;
            _rate = 1000000 / (PixelDwelTime + LowTime);//1000000（单位：微秒，即1秒）/（每个像素的持续时间+低电平时间）

            //计算波形
            _waveformArray = null;
            _waveformArray = new double[_sampsPerChan * 2];//x、y双通道
            double waveformXFactor = (Max_x_v - Min_x_v) / (XPts - 1);
            double waveformYFactor = (Max_y_v - Min_y_v) / (YPts - 1);
            for (int i = 0; i < YPts; i++)
            {
                if (i % 2 == 0)
                {
                    for (int j = 0; j < XPts; j++)
                    {
                        _waveformArray[_sampsPerChan + i * XPts + j] = waveformXFactor * j + Min_x_v;

                        if ((i + 1) < YPts)
                        {
                            if (Convert.ToBoolean(WaveModel))
                            {
                                _waveformArray[_sampsPerChan + (i + 2) * XPts - (j + 1)] = waveformXFactor * j + Min_x_v;//三角波，Z字形
                            }
                            else
                            {
                                _waveformArray[_sampsPerChan + (i + 1) * XPts + j] = waveformXFactor * j + Min_x_v;//锯齿波，循环至第一位置
                            }
                        }
                    }
                }
                for (int j = 0; j < XPts; j++)
                {
                    _waveformArray[i * XPts + j] = waveformYFactor * i + Min_y_v;//X轴
                }
            }
        }

        #endregion

        public NationalInstruments.DAQmx.Task? AOTask { get; set; }

        public NationalInstruments.DAQmx.Task? AITask { get; set; }

        public string? AoSource { get; set; } = "Dev1/ao0:1";//输出，振镜，默认双通道

        public string? AiSource { get; set; } = "Dev1/ai2";

        public string? TriggerSource { get; set; } = "/Dev1/ao/StartTrigger";//时钟触发，同步

        public bool GetDevices(out List<string> devices)
        {
            devices = new List<string>();
            try
            {
                 
                string[] deviceNames = DaqSystem.Local.Devices;
                foreach (string device in deviceNames)
                {
                    devices.Add(device);
                }
                return true;
            }
            catch (DaqException ex)
            {
                Debug.WriteLine($"GetDevices DAQmx Exception: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        public bool CreateAoTask()
        {
            string taskName = $"AOStart{DateTime.Now.ToString("fff")}";
            try
            {
                if (!Valid()) return false;
                AOTask = null;
                AOTask = new NationalInstruments.DAQmx.Task(taskName);
                AOTask.AOChannels.CreateVoltageChannel(AoSource, "",  MinAO,  MaxAO, AOVoltageUnits.Volts);//创建电压输出通道
                AOTask.Timing.ConfigureSampleClock("",  _rate, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples,  _sampsPerChan);
                //配置数据采样时钟，rate始终速率-每秒采样数量，下降沿触发，samples采样模式-有限样本，samplesPerChannel各通道样本数量（有限样本模式下）

                var writerMul = new AnalogMultiChannelWriter(AOTask?.Stream);//NI设置：Analog-multichannel-multisamples-1DWaveform
                if ( _waveformArray == null) return false;
                double[]? firstHalf =  _waveformArray.Take( _sampsPerChan).ToArray();
                double[]? secondHalf =  _waveformArray.Skip( _sampsPerChan).ToArray();
                double[,]? newArray = CombineWaveArrays(firstHalf, secondHalf);

                AnalogWaveform<double>[] waveformArrayTram = new AnalogWaveform<double>[ _waveformArray.Length];
                waveformArrayTram = AnalogWaveform<double>.FromArray2D(newArray);
                writerMul.WriteWaveform(false, waveformArrayTram);

                return true;
            }
            catch (DaqException ex)
            {
                Debug.WriteLine($"CreateAoTask DAQmx Exception: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("CreateAoTask" + ex.Message);
                return false;
            }
        }

        public bool CreateAiTask()
        {
            try
            {
                if (!Valid()) return false;
                AITask = null;
                AITask = new NationalInstruments.DAQmx.Task("AIStart");
                AITask.AIChannels.CreateVoltageChannel(AiSource, "", (AITerminalConfiguration)(-1),  MinAI,  MaxAI, AIVoltageUnits.Volts);
                AITask.Timing.ConfigureSampleClock("",  _rate, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples,  _sampsPerChan);
                AITask.Triggers.StartTrigger.ConfigureDigitalEdgeTrigger(TriggerSource, DigitalEdgeStartTriggerEdge.Rising);//信号同步
                AITask?.Control(TaskAction.Verify);
                return true;
            }
            catch (DaqException ex)
            {
                Debug.WriteLine($"CreateAiTask DAQmx Exception: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("CreateAiTask" + ex.Message);
                return false;
            }
        }

        public bool StartTask(byte index)
        {           
            try
            {
                var task = index == 0 ? AOTask : AITask;
                task?.Start();
                return true;
            }
            catch (DaqException ex)
            {
                Debug.WriteLine($"StartTask DAQmx Exception: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("StartTask" + ex.Message);
                return false;
            }
        }

        public bool StopTask(byte index)
        {
             
            try
            {
                var task = index == 0 ? AOTask : AITask;
                task?.Stop();
                return true;
            }
            catch (DaqException ex)
            {
                Debug.WriteLine($"StopTask DAQmx Exception: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("StopTask" + ex.Message);
                return false;
            }
        }

        public bool WaitUntilDone(byte index, int millisecond)
        {
             
            var task = index == 0 ? AOTask : AITask;
            try
            {
                if (task == null) return false;
                if (millisecond == 0)
                {
                    task?.WaitUntilDone();
                }
                else
                {
                    task?.WaitUntilDone(millisecond);
                }
                return true;
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine($"WaitUntilDone-TimeoutException: {ex.Message}");
                return false;
            }
            catch (ObjectDisposedException)
            {
                // 如果任务已经被释放，则在这里处理 ObjectDisposedException 异常
                Debug.WriteLine($"Task-{task} has already been disposed.");
                return false;
            }
            catch (DaqException ex)
            {
                // 处理 DAQmx 异常
                Debug.WriteLine("DisposeTask DAQmxException occurred: " + ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WaitUntilDone: {ex.Message}");
                return false;
            }
        }
        public bool DisposeTask(byte index)
        {
            var task = index == 0 ? AOTask : AITask;
            try
            {
                task?.Dispose();
                task = index == 0 ? AOTask = null : AITask = null;
                return true;
            }
            catch (ObjectDisposedException)
            {
                // 如果任务已经被释放，则在这里处理 ObjectDisposedException 异常
                Debug.WriteLine($"Task-{task} has already been disposed.");
                return false;
            }
            catch (DaqException ex)
            {
                // 处理 DAQmx 异常
                Debug.WriteLine("DisposeTask DAQmxException occurred: " + ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                // 处理其他类型的异常
                Debug.WriteLine("DisposeTask： " + ex.Message);
                return false;
            }
        }

        private bool Valid()
        {
            if (!GetDevices(out List<string> devices) || devices.Count == 0) return false;

            string device = devices[0];//默认一个通道
            AoSource = AoSource?.Replace("Dev1", device);  //输出，振镜，双通道
            AiSource = AiSource?.Replace("Dev1", device); 
            TriggerSource = TriggerSource?.Replace("Dev1", device);//触发
            return true;
        }

        public bool ImageDataOutput(out List<Mat> mats)
        {
            mats = new();
            try
            {
                if (!GetImageOriginData(out List<double[]> imageDataList)) return false;

                for (int i = 0; i < imageDataList.Count; i++)
                {
                    if (!ConverterImageData(imageDataList[i], out Mat matImage)) return false;

                    mats.Add(matImage);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ImageDataOutput Error:" + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 获取原始图像数据
        /// </summary>
        /// <param name="imageDataList"></param>
        /// <returns></returns>
        private bool GetImageOriginData(out List<double[]> imageDataList)
        {
            imageDataList = new List<double[]>();
            try
            {
                if (AITask == null || AOTask == null || AITask?.Stream == null) return false;
                AnalogMultiChannelReader reader = new AnalogMultiChannelReader(AITask?.Stream);
                double[,]? dataMul = reader.ReadMultiSample( _sampsPerChan);
                var row = dataMul.GetLength(0);
                var col = dataMul.GetLength(1);
                for (int i = 0; i < row; i++)
                {
                    double[] imageData = new double[ _sampsPerChan];
                    for (int j = 0; j < col; j++)
                    {
                        imageData[j] = dataMul[i, j];
                    }
                    imageDataList.Add(imageData);
                }
                return true;

            }
            catch (DaqException ex)
            {
                Debug.WriteLine($"GetImageOriginData DAQmx Exception: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("GetImageOriginData" + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 转换图像输出
        /// </summary>
        /// <param name="imageData"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="resultArray"></param>
        /// <returns></returns>
        private bool ConverterImageData(double[] imageData, out Mat mat)
        {
            int width = XPts - XMargin;
            int height = YPts - YMargin;
            mat = new(height, width, MatType.CV_32FC1);//CV_16SC1，CV_32FC1、CV_8UC1,其餘數據為0.待優化
            float[] outputData = new float[width * height];//short、float、byte
            try
            {
                if (imageData.Length < width * height)
                {
                    Debug.WriteLine("Invalid image data size.");
                    return false;
                }

                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        //1、计算待填入的新数组的序号
                        int arrayIndex = i * width + j;

                        //2、计算待填入的图像信息数组的序号
                        int imageIndex = 0;
                        if (Convert.ToBoolean(WaveModel))//三角波
                        {
                            if (i % 2 == 0)
                            {
                                //偶数行
                                imageIndex = i * XPts + j + XMargin;
                            }
                            else
                            {
                                //奇数行，前边若干位舍弃，后边数逐一向前补齐，因增加了X的偏移，有效图像数量能够对应。
                                //若XOffsetforTriangle为0，奇数行图像和偶数行交叉，显示重影
                                imageIndex = i * XPts + (width - j - 1 ) + XOffsetforTriangle;
                            }
                        }
                        else//锯齿波
                        {
                            imageIndex = i * XPts + j + XMargin;
                        }

                        //3、数据填充
                        double data = imageData[imageIndex];
                        if (double.IsNaN(data)) data = 0;

                        outputData[arrayIndex] = (float)data;//short、float、byte
                    }
                }
               
                Marshal.Copy(outputData, 0, mat.Data, width * height);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ConverterImageData" + ex.Message);
                return false;
            }       
        }

        /// <summary>
        /// 合并数组
        /// </summary>
        /// <param name="array1"></param>
        /// <param name="array2"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private double[,] CombineWaveArrays(double[] array1, double[] array2)
        {
            try
            {
                int length = array1.Length;
                double[,] resultArray = new double[2, length];
                for (int i = 0; i < array1.Length; i++)
                {
                    resultArray[0, i] = array1[i];
                    resultArray[1, i] = array2[i];
                }
                return resultArray;
            }
            catch (Exception)
            {
                throw new Exception();
            }
        }

        private void ArrayTransferinTriggleWave(int removeCount, short[] inputArray, out short[] outputArray)
        {
            outputArray = new short[inputArray.Length];

            // 舍弃前removeCount个数字       
            short[] newArray = new short[inputArray.Length - removeCount];
            Array.Copy(inputArray, removeCount, newArray, 0, newArray.Length);

            //将剩余数字前移
            Array.Copy(newArray, 0, inputArray, 0, newArray.Length);

            //末位的removeCount个数字补零
            Array.Clear(inputArray, newArray.Length, 10);

            outputArray = inputArray;

        }
    }
}
