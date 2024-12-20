using OpenCvSharp;
using System.Runtime.InteropServices;

namespace PointScan_Test
{
    class NICamera
    {
        #region setting
        public int WaveModel
        {
            get
            {
              return  _NIControl.WaveModel;
            }
            set
            {
                _NIControl.WaveModel = value;
            }
        }

        /// <summary>
        /// 原始X位置
        /// </summary>
        public int OriginX
        {
            get
            {
                return _NIControl.OriginX;
            }
            set
            {
                _NIControl.OriginX = value;
            }
        }
        /// <summary>
        /// 原始Y位置
        /// </summary>
        public int OriginY
        {
            get
            {
                return _NIControl.OriginY;
            }
            set
            {
                _NIControl.OriginY = value;
            }
        }
        /// <summary>
        /// X轴最大电压
        /// </summary>
        public double Max_x_v
        {
            get
            {
                return _NIControl.Max_x_v;
            }
            set
            {
                _NIControl.Max_x_v = value;
            }
        }
        /// <summary>
        /// X轴最小电压
        /// </summary>
        public double Min_x_v
        {
            get
            {
                return _NIControl.Min_x_v;
            }
            set
            {
                _NIControl.Min_x_v = value;
            }
        }
        /// <summary>
        /// Y轴最大电压
        /// </summary>
        public double Max_y_v
        {
            get
            {
                return _NIControl.Max_y_v;
            }
            set
            {
                _NIControl.Max_y_v = value;
            }
        }
        /// <summary>
        /// Y轴最小电压
        /// </summary>
        public double Min_y_v
        {
            get
            {
                return _NIControl.Min_y_v;
            }
            set
            {
                _NIControl.Min_y_v = value;
            }
        }
        /// <summary>
        /// 低电平时间
        /// </summary>
        public double LowTime
        {
            get
            {
                return _NIControl.LowTime;
            }
            set
            {
                _NIControl.LowTime = value;
            }
        }
        /// <summary>
        /// 像素停留时间
        /// </summary>
        public double PixelDwelTime
        {
            get
            {
                return _NIControl.PixelDwelTime;
            }
            set
            {
                _NIControl.PixelDwelTime = value;
            }
        }
        #endregion

        public List<string>? _devices;
        private readonly NIControl _NIControl;
        private Mat? CurrentFrameforSaving { get; set; }

        public NICamera()
        {
            _NIControl = new NIControl();
        }

        public bool Init()
            => _NIControl.GetDevices(out _devices) && _devices.Count != 0;

        public bool Capture(out Mat mat)
        {
            mat = new Mat();
            try
            {
                if (!_NIControl.StartTask(1)) return false;
                if (!_NIControl.StartTask(0)) return false;
                if (!_NIControl.WaitUntilDone(0, 0)) return false;
                if (!_NIControl.WaitUntilDone(1, 0)) return false;
                if (_NIControl.ImageDataOutput(out List<Mat> mats))
                {     
                    if (mats.Count > 0) 
                        mat = mats[0];//取第一位置的图像

                    CurrentFrameforSaving = mat.Clone();
                }

                if (!_NIControl.StopTask(0)) return false;
                if (!_NIControl.StopTask(1)) return false;
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                GC.Collect();
            }
        }

        public bool StartCapture()
        {
            _NIControl.SetParamandGenWaveform();
            if (!_NIControl.CreateAiTask()) return false;
            if (!_NIControl.CreateAoTask()) return false;
            return true;
        }

        public bool StopCapture()
        {
            //卡线程，等待结束
            _NIControl?.DisposeTask(0);
            _NIControl?.DisposeTask(1);
           return true;
        }

        public bool SaveCapture(string path)
        {
            try
            {
                if (CurrentFrameforSaving == null || CurrentFrameforSaving.Cols == 0 || CurrentFrameforSaving.Rows == 0)
                {
                    Console.WriteLine("Get Frame Error.————————Save");
                    return false;
                }
                if (!CurrentFrameforSaving.SaveImage(path)) return false;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine( ex.Message);
                return false;
            }
        }

    }
}
