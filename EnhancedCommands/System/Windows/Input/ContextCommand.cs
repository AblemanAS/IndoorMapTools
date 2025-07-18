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

        public virtual bool CanExecute(object parameter) => true;
        public abstract void Execute(object parameter);
    }
}
