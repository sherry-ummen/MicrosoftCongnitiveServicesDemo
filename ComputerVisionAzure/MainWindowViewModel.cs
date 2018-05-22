using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ComputerVisionAzure.Service;

namespace ComputerVisionAzure {
    internal class MainWindowViewModel : ViewModelBase {
        private readonly ComputeVisionService _computeVisionService;
        public Action<object> OnResult { get; set; }

        public MainWindowViewModel() {
            _computeVisionService = new ComputeVisionService();
        }

        public ICommand OnClickCommand => AsyncCommand.Create(async (type) => { await OnClick(type); });

        private async Task OnClick(object type) {
            var result = await _computeVisionService.Analyze(type.ToString());
            OnResult(result);
        }

    }
}
