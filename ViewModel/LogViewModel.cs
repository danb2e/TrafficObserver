using IotechiCore.WPF.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IotechiCore.Mediator.Colleague;
using EventLogLib;
using System.ComponentModel;
using IotechiCore.WPF.Mvvm.Tools;

namespace TrafficObserver.View
{
    class LogViewModel : ViewModelBase, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private RoutedIotechiCoreColleague<EventLogParam> EventLogColleague = null;
        public ObservableList<EventLogParam> LogList { get { return (ObservableList<EventLogParam>)this["LogList"]; } set { this["LogList"] = value; } }

        public LogViewModel()
        {
            LogList = new ObservableList<EventLogParam>();
            EventLogColleague = new RoutedIotechiCoreColleague<EventLogParam>(
                SystemLogProvider.Instance().LogName, "LogViewModel", EventLogMediator.Instance(), EventLogWrite);
        }

        private void EventLogWrite(EventLogParam data)
        {
            LogList.Add(data);
            //OnPropertyChanged(""LogList);
        }

        private void OnPropertyChanged(string info)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(info));
            }
        }

    }
}
