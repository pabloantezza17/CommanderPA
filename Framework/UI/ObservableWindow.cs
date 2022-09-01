using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using MahApps.Metro.Controls;

namespace Framework.UI
{
    public class ObservableWindow : MetroWindow, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChangedEvent(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableWindow() : base()
        {
            this.BorderThickness = new Thickness(1);

            this.BorderBrush = Brushes.Black;
        }
    }
}