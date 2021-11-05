using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Fabolus_v16.MVVM.ViewModels {
    public class ViewModelBase : INotifyPropertyChanged {

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public virtual void Dispose() { }

        public virtual void OnExit() { }
    }
}
