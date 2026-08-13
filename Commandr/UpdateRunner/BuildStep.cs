using System;
using System.ComponentModel;

namespace Commandr.UpdateRunner
{
    public enum StepState
    {
        Pending,
        Running,
        Ok,
        Failed,

        /// <summary>Estaba corriendo cuando se frenó el proceso.</summary>
        Cancelled,

        /// <summary>Nunca arrancó porque se frenó el proceso antes de llegar.</summary>
        Skipped
    }

    public class BuildStep : INotifyPropertyChanged
    {
        #region Members

        public event PropertyChangedEventHandler PropertyChanged;

        private StepState _state;

        #endregion

        #region Constructor

        public BuildStep(String name, String solutionPath = null)
        {
            this.Name = name;
            this.SolutionPath = solutionPath;
            this._state = StepState.Pending;
        }

        #endregion

        #region Properties

        public String Name { get; private set; }

        /// <summary>Ruta de la solución a compilar; null en pasos que no son build (get latest, tests).</summary>
        public String SolutionPath { get; private set; }

        public StepState State
        {
            get { return this._state; }
            set
            {
                this._state = value;
                this.RaisePropertyChangedEvent("State");
            }
        }

        #endregion

        #region Methods

        private void RaisePropertyChangedEvent(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
