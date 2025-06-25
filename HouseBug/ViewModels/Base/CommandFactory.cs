using System;
using System.Windows.Input;

namespace HouseBug.ViewModels.Base
{
    public static class CommandFactory
    {
        public static ICommand Create(Action execute, Func<bool> canExecute = null)
        {
            return new RelayCommand(execute, canExecute);
        }

        public static ICommand Create<T>(Action<T> execute, Predicate<T> canExecute = null)
        {
            return new RelayCommand<T>(execute, canExecute);
        }
    }
}
