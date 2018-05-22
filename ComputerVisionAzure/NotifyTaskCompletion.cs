using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace ComputerVisionAzure {
    // Watches a task and raises property-changed notifications when the task completes.
    public sealed class NotifyTaskCompletion<TResult> : INotifyPropertyChanged {
        public NotifyTaskCompletion(Task<TResult> task) {
            Task = task;
            RaisePropertyChanged = propertyName => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            TaskCompletion = WatchTaskAsync(task);
        }

        private Action<string> RaisePropertyChanged { get; }

        private async Task WatchTaskAsync(Task task) {
            try {
                await task;
            } catch {
            }

            RaisePropertyChanged(nameof(Status));
            RaisePropertyChanged(nameof(IsCompleted));
            RaisePropertyChanged(nameof(IsNotCompleted));
            if (task.IsCanceled) {
                RaisePropertyChanged(nameof(IsCanceled));
            } else if (task.IsFaulted) {
                RaisePropertyChanged(nameof(IsFaulted));
                RaisePropertyChanged(nameof(Exception));
                RaisePropertyChanged(nameof(InnerException));
                RaisePropertyChanged(nameof(ErrorMessage));
            } else {
                RaisePropertyChanged(nameof(IsSuccessfullyCompleted));
                RaisePropertyChanged(nameof(Result));
            }
        }
        public Task<TResult> Task { get; }
        public Task TaskCompletion { get; }
        public TResult Result => (Task.Status == TaskStatus.RanToCompletion) ?
            Task.Result : default(TResult);

        public TaskStatus Status => Task.Status;
        public bool IsCompleted => Task.IsCompleted;
        public bool IsNotCompleted => !Task.IsCompleted;

        public bool IsSuccessfullyCompleted => Task.Status == TaskStatus.RanToCompletion;

        public bool IsCanceled => Task.IsCanceled;
        public bool IsFaulted => Task.IsFaulted;
        public AggregateException Exception => Task.Exception;

        public Exception InnerException => Exception?.InnerException;

        public string ErrorMessage => InnerException?.Message;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
