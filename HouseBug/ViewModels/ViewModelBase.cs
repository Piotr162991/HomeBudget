using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace HouseBug.ViewModels.Base
{
    public abstract class ViewModelBase : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        private string _busyMessage;
        public string BusyMessage
        {
            get => _busyMessage;
            set => SetProperty(ref _busyMessage, value);
        }

        private string _validationMessage;
        public string ValidationMessage
        {
            get => _validationMessage;
            set => SetProperty(ref _validationMessage, value);
        }

        protected virtual void SetBusy(bool isBusy, string message = "Ładowanie...")
        {
            IsBusy = isBusy;
            BusyMessage = message;
        }

        protected async Task HandleOperationAsync(string operationName, Func<Task> operation)
        {
            SetBusy(true, $"Wykonywanie operacji: {operationName}...");
            try
            {
                await operation();
                ValidationMessage = $"Operacja {operationName} zakończona pomyślnie.";
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Błąd podczas {operationName}: {ex.Message}";
            }
            finally
            {
                SetBusy(false);
            }
        }

        protected async Task<T> HandleOperationAsync<T>(string operationName, Func<Task<T>> operation, T defaultValue = default)
        {
            SetBusy(true, $"Wykonywanie operacji: {operationName}...");
            try
            {
                var result = await operation();
                ValidationMessage = $"Operacja {operationName} zakończona pomyślnie.";
                return result;
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Błąd podczas {operationName}: {ex.Message}";
                return defaultValue;
            }
            finally
            {
                SetBusy(false);
            }
        }

        protected void InitializeCollection<TItem>(ObservableCollection<TItem> collection, IEnumerable<TItem> items)
        {
            collection.Clear();
            foreach (var item in items)
            {
                collection.Add(item);
            }
        }

        #region IDisposable

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                }

                _disposed = true;
            }
        }

        #endregion
    }
}