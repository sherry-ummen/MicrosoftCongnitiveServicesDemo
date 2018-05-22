using System.Threading.Tasks;
using System.Windows.Input;

namespace ComputerVisionAzure {
    public interface IAsyncCommand : ICommand {
        Task ExecuteAsync(object parameter);
    }
}
