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
            // 데이터 추가되었을때 테스트 하기위해 임의로 추가한 곳
            EventLogParam ev = new EventLogParam();
            ev.RunType = "검출";
            ev.EventTime = DateTime.Now;
            ev.EventTimeStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            ev.LogLevel = 0;
            ev.Message = "검출 시작";

            for (int i = 0; i < 5; i++)
            {
                LogList.Add(ev);
            }
            // -- end.
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
