using IotechiCore.Mediator.Colleague;
using IotechiCore.WPF.Mvvm;
using IotechiCore.WPF.Mvvm.Tools;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using static IotechiCore.WPF.Module.ProcessLayoutManager;



namespace TrafficObserver.View
{
    enum ButtonMode : int
    {
        ModeNone,
        AddGCP,
        EditGCP,
        MoveGCP,
        DeleteGCP,
        AddPoint,
        AddPointExit,
        MovePoint,
        DeletePoint
    }
    class MenuViewModel : ViewModelBase
    {
        #region Command
        public ICommand OpenVideo { get { return (ICommand)this["OpenVideo"]; } set { this["OpenVideo"] = value; } }
        public ICommand AddTrack { get { return (ICommand)this["AddTrack"]; } set { this["AddTrack"] = value; } }
        public ICommand AddGCP { get { return (ICommand)this["AddGCP"]; } set { this["AddGCP"] = value; } }
        public ICommand EditGCP { get { return (ICommand)this["EditGCP"]; } set { this["EditGCP"] = value; } }
        public ICommand MoveGCP { get { return (ICommand)this["MoveGCP"]; } set { this["MoveGCP"] = value; } }
        public ICommand DeleteGCP { get { return (ICommand)this["DeleteGCP"]; } set { this["DeleteGCP"] = value; } }
        public ICommand SelectTrack { get { return (ICommand)this["SelectTrack"]; } set { this["SelectTrack"] = value; } }
        public string TextSelectedTrack { get { return (string)this["TextSelectedTrack"]; } set { this["TextSelectedTrack"] = value; OnPropertyChanged("TextSelectedTrack"); } }
        public string TextAddPoint { get { return (string)this["TextAddPoint"]; } set { this["TextAddPoint"] = value; OnPropertyChanged("TextAddPoint"); } }
        public ICommand AddPoint { get { return (ICommand)this["AddPoint"]; } set { this["AddPoint"] = value; } }
        public ICommand MovePoint { get { return (ICommand)this["MovePoint"]; } set { this["MovePoint"] = value; } }
        public ICommand DeletePoint { get { return (ICommand)this["DeletePoint"]; } set { this["DeletePoint"] = value; } }
        public ICommand SavePoints { get { return (ICommand)this["SavePoints"]; } set { this["SavePoints"] = value; } }
        public ICommand LoadPoints { get { return (ICommand)this["LoadPoints"]; } set { this["LoadPoints"] = value; } }
        public ICommand OpenWeight { get { return (ICommand)this["OpenWeight"]; } set { this["OpenWeight"] = value; } }
        public ICommand OpenConfig { get { return (ICommand)this["OpenConfig"]; } set { this["OpenConfig"] = value; } }
        public ICommand OpenName { get { return (ICommand)this["OpenName"]; } set { this["OpenName"] = value; } }
        
        public ICommand StartPauseTrajactory { get { return (ICommand)this["StartPauseTrajactory"]; } set { this["StartPauseTrajactory"] = value; } }

        #endregion
        #region Variable
        public event PropertyChangedEventHandler PropertyChanged;
        string DecodedImageFolder;

        bool Processing;
        int NumTotalTrack;
        int SelectedTrack;

        int curMode;
        #endregion

        public MenuViewModel()
        {
            #region Initialization
            OpenVideo = new RelayCommand(new Action(OpenVideoAction));
            AddTrack = new RelayCommand(new Action(AddTrackAction));
            AddGCP = new RelayCommand(new Action(AddGCPAction));
            MoveGCP = new RelayCommand(new Action(MoveGCPAction));
            EditGCP = new RelayCommand(new Action(EditGCPAction));
            DeleteGCP = new RelayCommand(new Action(DeleteGCPAction));
            SelectTrack = new RelayCommand(new Action(SelectTrackAction));
            TextSelectedTrack = "통행로 선택 : 전체";
            TextAddPoint = "차선 추가 : 출입";
            AddPoint = new RelayCommand(new Action(AddPointAction));
            MovePoint = new RelayCommand(new Action(MovePointAction));
            DeletePoint = new RelayCommand(new Action(DeletePointAction));
            SavePoints = new RelayCommand(new Action(SavePointsAction));
            LoadPoints = new RelayCommand(new Action(LoadPointsAction));
            OpenWeight = new RelayCommand(new Action(OpenWeightAction));
            OpenConfig = new RelayCommand(new Action(OpenConfigAction));
            OpenName = new RelayCommand(new Action(OpenNameAction));

            StartPauseTrajactory = new RelayCommand(new Action(StartPauseTrajactoryAction));

            DecodedImageFolder = Directory.GetCurrentDirectory() + @"\DecodedFrames";
            DirectoryInfo FolderConfirm = new DirectoryInfo(DecodedImageFolder);

            curMode = 0;

            NumTotalTrack = 0;
            SelectedTrack = 0;

            if (FolderConfirm.Exists == false)
            {
                FolderConfirm.Create();
            }
            else
            {
                if (FolderConfirm.GetFiles("*.*", System.IO.SearchOption.AllDirectories).Length != 0)
                {
                    CleanFolder(DecodedImageFolder);
                }
            }

            Processing = false;
            #endregion
        }

        #region Command Action
        private void OpenVideoAction()
        {
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
            
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                Mediator.Mediator.Instance().Distribute("OpenVideoAction", ofd.FileName);
            }
            else
                System.Windows.MessageBox.Show("파일이 선택되지 않았습니다. 다시 시도해주세요.", "에러 메시지", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        private void AddTrackAction() { Mediator.Mediator.Instance().Distribute("AddTrackAction", ++NumTotalTrack); }
        private void AddGCPAction() { curMode = curMode == (int)ButtonMode.AddGCP ? (int)ButtonMode.ModeNone : (int)ButtonMode.AddGCP; Mediator.Mediator.Instance().Distribute("ModeChangeAction", curMode); }
        private void MoveGCPAction() { curMode = curMode == (int)ButtonMode.MoveGCP ? (int)ButtonMode.ModeNone : (int)ButtonMode.MoveGCP; Mediator.Mediator.Instance().Distribute("ModeChangeAction", curMode); }
        private void EditGCPAction() { curMode = curMode == (int)ButtonMode.EditGCP ? (int)ButtonMode.ModeNone : (int)ButtonMode.EditGCP; Mediator.Mediator.Instance().Distribute("ModeChangeAction", curMode); }
        private void DeleteGCPAction() { curMode = curMode == (int)ButtonMode.DeleteGCP ? (int)ButtonMode.ModeNone : (int)ButtonMode.DeleteGCP; Mediator.Mediator.Instance().Distribute("ModeChangeAction", curMode); }
        private void AddPointAction() { curMode = curMode == (int)ButtonMode.AddPoint ? (int)ButtonMode.AddPointExit : (int)ButtonMode.AddPoint; TextAddPoint = curMode == (int)ButtonMode.AddPoint ? "차선 추가 : 출입" : "차선 추가 : 출구"; Mediator.Mediator.Instance().Distribute("ModeChangeAction", curMode); }
        private void MovePointAction() { curMode = curMode == (int)ButtonMode.MovePoint ? (int)ButtonMode.ModeNone : (int)ButtonMode.MovePoint; Mediator.Mediator.Instance().Distribute("ModeChangeAction", curMode); }
        private void DeletePointAction() { curMode = curMode == (int)ButtonMode.DeletePoint ? (int)ButtonMode.ModeNone : (int)ButtonMode.DeletePoint; Mediator.Mediator.Instance().Distribute("ModeChangeAction", curMode); }
        private void SelectTrackAction() 
        {
            if (SelectedTrack == NumTotalTrack)
                SelectedTrack = 0;
            else
                SelectedTrack++;

            TextSelectedTrack = "통행로 선택 : " + (SelectedTrack == 0 ? "전체" : SelectedTrack.ToString());
            Mediator.Mediator.Instance().Distribute("SelectTrackAction", SelectedTrack);
        }
        private void SavePointsAction()
        {
            System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog();
            sfd.Filter = "Text File|*.txt";
            sfd.AddExtension = true;
            sfd.DefaultExt = ".txt";

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                Mediator.Mediator.Instance().Distribute("SavePointsAction", sfd.FileName);
            }
            else
                System.Windows.MessageBox.Show("파일이 선택되지 않았습니다. 다시 시도해주세요.", "에러 메시지", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        private void LoadPointsAction()
        {
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string[] textValue = null;
                textValue = System.IO.File.ReadAllLines(ofd.FileName);
                string[] item = textValue[0].Split(' ');
                NumTotalTrack = Convert.ToInt32(item[1]);
                Mediator.Mediator.Instance().Distribute("LoadPointsAction", ofd.FileName);
            }
            else
                System.Windows.MessageBox.Show("파일이 선택되지 않았습니다. 다시 시도해주세요.", "에러 메시지", MessageBoxButton.OK, MessageBoxImage.Warning);
            //Save Point
            //Load Point
        }
        private void OpenWeightAction()
        {
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                Mediator.Mediator.Instance().Distribute("OpenWeightAction", ofd.FileName);
            }
            else
                System.Windows.MessageBox.Show("파일이 선택되지 않았습니다. 다시 시도해주세요.", "에러 메시지", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        private void OpenConfigAction()
        {
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                Mediator.Mediator.Instance().Distribute("OpenConfigAction", ofd.FileName);
            }
            else
                System.Windows.MessageBox.Show("파일이 선택되지 않았습니다. 다시 시도해주세요.", "에러 메시지", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        private void OpenNameAction()
        {
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                Mediator.Mediator.Instance().Distribute("OpenNameAction", ofd.FileName);
            }
            else
                System.Windows.MessageBox.Show("파일이 선택되지 않았습니다. 다시 시도해주세요.", "에러 메시지", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        private void StartPauseTrajactoryAction()
        {
            Processing = !Processing;
            Mediator.Mediator.Instance().Distribute("StartPauseTrajactoryAction", Processing);
        }
        #endregion
        private void CleanFolder(string target)
        {
            //File.Delete(target);

            //File.Create(target);
        }
        private void OnPropertyChanged(string info)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(info));
            }
        }

      //  private Uri minimize = new Uri("../Image/Top/mo_minimize.png");
        static BitmapImage minimizeToWindow = new BitmapImage(new Uri("../../../Image/Top/nor_minimize.png", UriKind.Relative));
        static BitmapImage shrinkToWindow = new BitmapImage(new Uri("../../../Image/Top/nor_shrink.png", UriKind.Relative));
        static BitmapImage closeToWindow = new BitmapImage(new Uri("../../../Image/Top/nor_Close.png", UriKind.Relative));

        public BitmapImage MinimizeToWindow
        {
            get { return minimizeToWindow; }
            set
            {
                minimizeToWindow = value;
                OnPropertyChanged("MinimizeToWindow");
            }
        }
        public BitmapImage ShrinkToWindow
        {
            get { return shrinkToWindow; }
            set
            {
                shrinkToWindow = value;
                OnPropertyChanged("ShrinkToWindow");
            }
        }
        public BitmapImage CloseToWindow
        {
            get { return closeToWindow; }
            set
            {
                closeToWindow = value;
                OnPropertyChanged("CloseToWindow");
            }
        }
    }
}
