using System.Windows;
using System.Windows.Media.Imaging;

namespace ComputerVisionAzure {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
            var viewModel = new MainWindowViewModel();
            viewModel.OnResult += (result) => Image.Source = (BitmapSource)result;
            DataContext = viewModel;
        }


    }
}
