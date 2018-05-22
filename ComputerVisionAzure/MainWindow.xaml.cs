using System;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using ComputerVisionAzure.Service;
using Microsoft.ProjectOxford.Face.Contract;
using OpenCvSharp.Extensions;

namespace ComputerVisionAzure {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
            var viewModel = new MainWindowViewModel();
            viewModel.OnResult += (result) => LeftImage.Source = (BitmapSource)result;
            DataContext = viewModel;
        }


    }
}
