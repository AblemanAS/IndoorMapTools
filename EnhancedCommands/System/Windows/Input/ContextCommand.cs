using System;
using System.Windows;
using System.Windows.Input;

namespace EnhancedCommands.System.Windows.Input
{
    public abstract class ContextCommand : Freezable, ICommand
    {
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public abstract bool CanExecute(object parameter);
        public abstract void Execute(object parameter);
    }
}
