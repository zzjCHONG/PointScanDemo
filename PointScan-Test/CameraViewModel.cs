using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PointScan_Test
{
    public partial class CameraViewModel : ObservableObject
    {
        private readonly NICamera _NICamera;

        public CameraViewModel()
        {
            _NICamera = new NICamera();
        }

        [ObservableProperty]
        private List<string>? _devices;

        [ObservableProperty]
        private int _devicesIndex = 0;

        public List<string> WaveList { get; set; } = new() { "锯齿波", "三角波", };

        [ObservableProperty]
        private int _waveSelectionIndex = 0;

        partial void OnWaveSelectionIndexChanged(int value)
        {
            _NICamera.WaveModel = value;
        }

        [ObservableProperty]
        private int _resolutionX = 0;//XPixelFactor

        partial void OnResolutionXChanged(int value)
        {
            _NICamera.OriginX = value;
        }

        [ObservableProperty]
        private int _resolutionY = 0;//YPixelFactor

        partial void OnResolutionYChanged(int value)
        {
            _NICamera.OriginY = value;
        }

        [ObservableProperty]
        private double _voltageSweepRangeUpperLimit = 0;//minXV,minYV

        partial void OnVoltageSweepRangeUpperLimitChanged(double value)
        {
            _NICamera.Max_x_v = value;
            _NICamera.Max_y_v = value;
        }

        [ObservableProperty]
        private double _voltageSweepRangeLowerLimit = 0;//maxXV,maxYV

        partial void OnVoltageSweepRangeLowerLimitChanged(double value)
        {
            _NICamera.Min_x_v = value;
            _NICamera.Min_y_v = value;
        }

        [ObservableProperty]
        private double _pixelDwelTime = 0;

        partial void OnPixelDwelTimeChanged(double value)
        {
            _NICamera.PixelDwelTime = value;
        }

        [ObservableProperty]
        private double _lowTime = 0;

        partial void OnLowTimeChanged(double value)
        {
            _NICamera.LowTime = value;
        }

        [ObservableProperty]
        private BitmapFrame? _bitmapSource;

        [RelayCommand]
        void Init()
        {
            if (_NICamera.Init())
            {
                Devices = _NICamera._devices;
                DevicesIndex = 0;

                LowTime = _NICamera.LowTime;
                PixelDwelTime = _NICamera.PixelDwelTime;
                ResolutionX = _NICamera.OriginX;
                ResolutionY = _NICamera.OriginY;
                VoltageSweepRangeLowerLimit = _NICamera.Min_x_v;
                VoltageSweepRangeUpperLimit = _NICamera.Max_x_v;

                WaveSelectionIndex = 0;
            }
            else
            {
                MessageBox.Show("初始化失败", "error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [ObservableProperty]
        private bool _isStartAcquisition = false;

        partial void OnIsStartAcquisitionChanged(bool value)
        {
            if (value)
            {
                Task.Run(() =>
                {
                    while (IsStartAcquisition)
                    {
                        Mat? mat = null;
                        try
                        {
                            if (_NICamera.Capture(out mat))
                            {
                                Application.Current?.Dispatcher.Invoke(() =>
                                {
                                    Cv2.Flip(mat, mat, FlipMode.Y);

                                    //BitmapSource = BitmapFrame.Create(mat.Clone().ToBitmapSource());

                                    mat.MinMaxLoc(out double max, out double min);
                                    if (max != 0 && min != 0)
                                        Display.Original = mat;
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        finally
                        {
                            mat?.Dispose();
                            GC.Collect();
                        }
                    }
                });
            }
        }

        [ObservableProperty]
        private bool _isSettingEnable = true;

        [RelayCommand]
        void StartCapture()
        {
            _NICamera.StartCapture();
            IsStartAcquisition = true;
            IsSettingEnable = false;
        }

        [RelayCommand]
        void StopCapture()
        {
            _NICamera.StopCapture();
            IsStartAcquisition = false;
            IsSettingEnable = true;
        }

        [ObservableProperty]
        private string _fileName = "";

        [ObservableProperty]
        private bool _timeSuffix = true;

        [RelayCommand]
        void SaveCapture()
        {
            var name = $"{FileName}" +
                 $"{(!string.IsNullOrEmpty(FileName) && TimeSuffix ? "_" : "")}" +
                 $"{(TimeSuffix ? $"{DateTime.Now:yyyyMMdd_HH_mm_ss}" : "")}";

            var dlg = new SaveFileDialog()
            {
                Title = "存储图片",
                FileName = name,
                Filter = "TIF|*.tif",
                DefaultExt = ".tif",
            };

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                _NICamera.SaveCapture(dlg.FileName);//保存原图
            }

        }

        [ObservableProperty]
        private DisplayModel_old _display = new();
    }
}
