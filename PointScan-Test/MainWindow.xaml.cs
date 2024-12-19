using FilterThrolabs;
using System.Windows;

namespace PointScan_Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ELL9 eLL9 = new();

        public MainWindow()
        {
            InitializeComponent();

            CameraViewModel cameraViewModel = new CameraViewModel();
            DataContext = cameraViewModel;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Application.Current.Shutdown();
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            eLL9.Home(true);
        }

        private void pos1_Click(object sender, RoutedEventArgs e)
        {
            eLL9.MovetoPos(1);
        }

        private void pos2_Click(object sender, RoutedEventArgs e)
        {
            eLL9.MovetoPos(2);
        }

        private void pos3_Click(object sender, RoutedEventArgs e)
        {
            eLL9.MovetoPos(3);
        }

        private void pos4_Click(object sender, RoutedEventArgs e)
        {
            eLL9.MovetoPos(4);
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            if (!eLL9.Connect(out string msg))
            {
                MessageBox.Show(msg);
            }
            else
            {
                MessageBox.Show("滑轨连接！");

            }
        }
    }
}