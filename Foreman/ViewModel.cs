namespace Foreman
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public abstract class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool SetProperty<T>(
            ref T field, T newValue, [CallerMemberName] string propertyName = null!)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
                return false;

            field = newValue;
            RaisePropertyChanged(propertyName);
            return true;
        }
    }
}
