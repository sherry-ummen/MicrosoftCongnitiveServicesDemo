using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ComputerVisionAzure {
    internal class ViewModelBase : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName]string propertyName = "") {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        protected void OnPropertyChanged(PropertyChangedEventArgs args) {
            var handler = PropertyChanged;
            handler?.Invoke(this, args);
        }
    }
}
