using System;
using System.Windows.Input;

namespace Framework.UI
{
    public class Command : ICommand
    {
        private readonly Action commandAction;

        private readonly Func<Boolean> canExecute;

        public event EventHandler CanExecuteChanged;

        public Command(Action commandAction, Func<Boolean> canExecute = null)
        {
            if (commandAction == null)
                throw new ArgumentException("CommandAction is required");

            this.commandAction = commandAction;
            this.canExecute = canExecute ?? (() => true);
        }

        public Boolean CanExecute(Object parameter)
        {
            return this.canExecute();
        }

        public void Execute(Object parameter)
        {
            this.commandAction();
        }

        public void RaiseCanExecuteChanged()
        {
            this.CanExecuteChanged?.Invoke(this, null);
        }
    }
}