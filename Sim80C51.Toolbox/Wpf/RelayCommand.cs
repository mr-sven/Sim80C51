﻿using System.Runtime.Serialization;
using System.Windows.Input;

namespace Sim80C51.Toolbox.Wpf
{
    [DataContract]
    public class RelayCommand(Action<object?> execute, Predicate<object?> canExecute) : ICommand
    {
        private event EventHandler? CanExecuteChangedInternal;

        public RelayCommand(Action<object?> execute) : this(execute, DefaultCanExecute) { }

        public event EventHandler? CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
                CanExecuteChangedInternal += value;
            }

            remove
            {
                CommandManager.RequerySuggested -= value;
                CanExecuteChangedInternal -= value;
            }
        }

        public bool CanExecute(object? parameter)
        {
            return canExecute != null && canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            execute(parameter);
        }

        public void OnCanExecuteChanged()
        {
            CanExecuteChangedInternal?.Invoke(this, EventArgs.Empty);
        }

        public void Destroy()
        {
            canExecute = _ => false;
            execute = _ => { return; };
        }

        private static bool DefaultCanExecute(object? parameter)
        {
            return true;
        }
    }
}
