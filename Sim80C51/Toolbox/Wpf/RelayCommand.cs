using System.Runtime.Serialization;
using System.Windows.Input;

namespace Sim80C51.Toolbox.Wpf
{
    [DataContract]
    public class RelayCommand : ICommand
    {
        private Action<object?> execute;

        private Predicate<object?> canExecute;

        private readonly Func<string> nameDetector = () => string.Empty;

        private event EventHandler? CanExecuteChangedInternal;

        [DataMember]
        public string Name { get { return nameDetector.Invoke(); } }


        public RelayCommand(Action<object?> execute) : this(() => string.Empty, execute, DefaultCanExecute) { }
        public RelayCommand(Func<string> name, Action<object?> execute) : this(name, execute, DefaultCanExecute) { }
        public RelayCommand(Action<object?> execute, Predicate<object?> canExecute) : this(() => string.Empty, execute, canExecute) { }
        public RelayCommand(Func<string> name, Action<object?> execute, Predicate<object?> canExecute)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
            nameDetector = name ?? throw new ArgumentNullException(nameof(name));
        }

        public event EventHandler? CanExecuteChanged
        {
            add
            {
                CanExecuteChangedInternal += value;
            }

            remove
            {
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
            EventHandler? handler = CanExecuteChangedInternal;
            handler?.Invoke(this, EventArgs.Empty);
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
