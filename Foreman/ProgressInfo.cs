namespace Foreman
{
    using System;

    public class ProgressInfo : ViewModel, IDisposable, IProgress<string>
    {
        private readonly Action<ProgressInfo> dispose;
        private readonly IProgress<string> progress;

        private ProgressType progressType = ProgressType.Indeterminate;
        private string? operation;
        private int currentItem;
        private int maximumItems;

        public ProgressInfo(Action<ProgressInfo> show, Action<ProgressInfo> dispose)
        {
            this.dispose = dispose;
            progress = new Progress<string>(x => Operation = x);
            show(this);
        }

        public ProgressType ProgressType
        {
            get => progressType;
            set => SetProperty(ref progressType, value);
        }

        public string? Operation
        {
            get => operation;
            set => SetProperty(ref operation, value);
        }

        public int CurrentItem
        {
            get => currentItem;
            set => SetProperty(ref currentItem, value);
        }

        public int MaximumItems
        {
            get => maximumItems;
            set => SetProperty(ref maximumItems, value);
        }

        public void Dispose()
        {
            dispose(this);
        }

        public void Report(string value)
        {
            progress.Report(value);
        }
    }

    public enum ProgressType
    {
        None,
        Indeterminate,
        Determinate
    }
}
