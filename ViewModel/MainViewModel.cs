using IotechiCore.WPF.Mvvm;
using IotechiCore.Mediator.Colleague;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.IO;
using System.Threading;
using LibFastYOLO;
using System.Windows;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using System.Windows.Controls;

namespace TrafficObserver.View
{
    public class MainViewModel : ViewModelBase
    {
        public struct TrackingObject
        {//tracking list //carID, List<Point> location, List<Point> utm, missing_count //추후 pixel block(luminance)
            public int ObjID;
            public List<int> ClassNum;
            public List<Rect> BoxInfo; //bounding box info
            public List<int> NumFrames; //탐지된 이미지 번호
            public List<Point> Center;
            public LibImageProcessing.KalmanFilter kalman; 
        }
        public struct TrackingPoints
        {
            public List<List<Point>> ListEnterTrack;
            public List<List<Point>> ListExitTrack;
        }

        LibFFMPEG.LibFFMPEG VideoDecoder;// = new LibFFMPEG.LibFFMPEG();
        LibFastYOLO.LibYOLO ObjectDetector;// = new LibFastYOLO.LibYOLO();
        LibImageProcessing.LibImageProcessing ImageProcesser;

        private List<RoutedIotechiCoreColleague<object>> ColleagueArray = new List<RoutedIotechiCoreColleague<object>>();

        public event PropertyChangedEventHandler PropertyChanged;
        public string ImgName { get { return (string)this["ImgName"]; } set { this["ImgName"] = value; OnPropertyChanged("ImgName"); } }

        Thread TrajactoryThread;
        Thread FFMPEGThread;
        Thread YOLOThread;
        bool TerminateTrajactoryThread;

        string SaveTrackingObjectFolder;
        string DecodedImageFolder;
        string NameVideoFile;
        string NameYOLOConfigFile;
        string NameYOLOWeightFile;
        string NameYOLOObjectNameFile;
        
        string NameCurrentFrame;
        Queue<string> QueueImageName; //Detection 완료된 이미지 큐
        Queue<BoundingBoxInfo> QueueDetection; //Detection을 위한 큐 (Detection 수행 전)

        bool Processing;
        public bool AllowTrajactory;

        int CurMode;
        int NumTrack;
        int SelectedTrack;
        public int LastFrameNum;

        private List<TrackingObject> TrackerList;
        public List<TrackingObject> DrawTrackerList;

        private List<Point> ListGCP; //GCP location
        private List<Point> ListInfoGCP; //GCP information (UTM coordinate)
        public List<Point> getListGCP() { return ListGCP; }
        public List<Point> getListInfoGCP() { return ListInfoGCP; }
        private TrackingPoints trackingPoints;
        public void EditInfoGCP(int idx, double utm_we, double utm_sn)
        {
            Point point = new Point(utm_we, utm_sn);
            ListInfoGCP[idx] = point;
        }

        public MainViewModel()
        {
            #region Initialization
            ColleagueArray.Add(new RoutedIotechiCoreColleague<object>("StartPauseTrajactoryAction", "MenuViewModel", Mediator.Mediator.Instance(), StartPauseTrajactoryAction));
            ColleagueArray.Add(new RoutedIotechiCoreColleague<object>("OpenVideoAction", "MenuViewModel", Mediator.Mediator.Instance(), OpenVideoAction));
            ColleagueArray.Add(new RoutedIotechiCoreColleague<object>("OpenConfigAction", "MenuViewModel", Mediator.Mediator.Instance(), OpenConfigAction));
            ColleagueArray.Add(new RoutedIotechiCoreColleague<object>("OpenWeightAction", "MenuViewModel", Mediator.Mediator.Instance(), OpenWeightAction));
            ColleagueArray.Add(new RoutedIotechiCoreColleague<object>("OpenNameAction", "MenuViewModel", Mediator.Mediator.Instance(), OpenNameAction));
            ColleagueArray.Add(new RoutedIotechiCoreColleague<object>("ModeChangeAction", "MenuViewModel", Mediator.Mediator.Instance(), ModeChangeAction));
            ColleagueArray.Add(new RoutedIotechiCoreColleague<object>("AddTrackAction", "MenuViewModel", Mediator.Mediator.Instance(), AddTrackAction));
            ColleagueArray.Add(new RoutedIotechiCoreColleague<object>("SelectTrackAction", "MenuViewModel", Mediator.Mediator.Instance(), SelectTrackAction));
            ColleagueArray.Add(new RoutedIotechiCoreColleague<object>("SavePointsAction", "MenuViewModel", Mediator.Mediator.Instance(), SavePointsAction));
            ColleagueArray.Add(new RoutedIotechiCoreColleague<object>("LoadPointsAction", "MenuViewModel", Mediator.Mediator.Instance(), LoadPointsAction));
            ColleagueArray.Add(new RoutedIotechiCoreColleague<object>("ProgramExit", "MenuViewModel", Mediator.Mediator.Instance(), ProgramExit));

            DecodedImageFolder = Directory.GetCurrentDirectory() + @"\DecodedFrames";
            SaveTrackingObjectFolder = Directory.GetCurrentDirectory() + @"\TrackingObjects";

            DirectoryInfo FolderConfirm = new DirectoryInfo(SaveTrackingObjectFolder);

            if (FolderConfirm.Exists == false)
            {
                FolderConfirm.Create();
            }

            TrajactoryThread = null;
            TerminateTrajactoryThread = true;

            Processing = false;
            AllowTrajactory = false;
            CurMode = 0; //GCP 추가, 변경, 차선을 위한 선 추가 등 모드 번호
            NumTrack = 0; //Track 개수
            SelectedTrack = 0; // 현재 선택된 Track (0은 전체)

            ImgName = "init(Sky).jpg";
            QueueImageName = new Queue<string>();
            QueueDetection = new Queue<BoundingBoxInfo>();
            NameCurrentFrame = "";
            NameYOLOConfigFile = "";
            NameYOLOWeightFile = "";
            NameYOLOObjectNameFile = "";
            ListGCP = new List<Point>();
            ListInfoGCP = new List<Point>();
            trackingPoints.ListEnterTrack = new List<List<Point>>();
            trackingPoints.ListExitTrack = new List<List<Point>>();
            LastFrameNum = 0;

            TrackerList = new List<TrackingObject>();
            DrawTrackerList = null;


            //ListPointsEnterTrack = new List<Point>();
            #endregion
        }
        private void ProgramExit(object arg)
        {
            TerminateTrajactoryThread = true;

            WaitThreadEnd();

            TrajactoryThread = null;
            FFMPEGThread = null;
            YOLOThread = null;
            ObjectDetector = null;
            VideoDecoder = null;
        }
        private void WaitThreadEnd()
        {
            bool IsAliveTrajactory = ThreadChecking(TrajactoryThread);
            bool IsAliveFFMPEGThread = ThreadChecking(FFMPEGThread);
            bool IsAliveYOLOThread = ThreadChecking(YOLOThread);

            while(IsAliveTrajactory || IsAliveFFMPEGThread || IsAliveYOLOThread)
            {
                IsAliveTrajactory = ThreadChecking(TrajactoryThread);
                IsAliveFFMPEGThread = ThreadChecking(FFMPEGThread);
                IsAliveYOLOThread = ThreadChecking(YOLOThread);
            }
        }
        private bool ThreadChecking(Thread thread)
        {
            return thread == null ? false : thread.IsAlive;
        }

        public void ReDrawImage()
        {
            string tmp = ImgName;
            ImgName = null;
            ImgName = tmp;
        }

        private void StartPauseTrajactoryAction(object arg)
        {
            if (ImgName == "init(Sky).jpg" || NameCurrentFrame == "" || NameYOLOConfigFile == "" || NameYOLOWeightFile == "" || NameYOLOObjectNameFile == "")
                return;//error
            Processing = (bool)arg;
            AllowTrajactory = true;

            if (Processing && TrajactoryThread == null)
            {
                TerminateTrajactoryThread = false;
                ObjectDetector = new LibFastYOLO.LibYOLO(NameYOLOConfigFile, NameYOLOWeightFile, NameYOLOObjectNameFile);
                ImageProcesser = new LibImageProcessing.LibImageProcessing();
                TrajactoryThread = new Thread(new ThreadStart(DoTrajactory));
                FFMPEGThread = new Thread(new ThreadStart(DoDecoding));
                YOLOThread = new Thread(new ThreadStart(DoDetection));
                TrajactoryThread.Start();
                FFMPEGThread.Start();
                YOLOThread.Start();
            }
        }

        private void OpenVideoAction(object arg) 
        { 
            NameVideoFile = (string)arg;
            VideoDecoder = new LibFFMPEG.LibFFMPEG();
            VideoDecoder.SetDecoderCUDA(NameVideoFile,DecodedImageFolder);
            NameCurrentFrame = ImgName = VideoDecoder.DecodeNextFrame();
            QueueImageName.Enqueue(VideoDecoder.DecodeNextFrame());
        }
        private void OpenConfigAction(object arg) { NameYOLOConfigFile = (string)arg; }
        private void OpenWeightAction(object arg) { NameYOLOWeightFile = (string)arg; }
        private void OpenNameAction(object arg) { NameYOLOObjectNameFile = (string)arg; }
        private void AddTrackAction(object arg) { NumTrack = (int)arg; trackingPoints.ListEnterTrack.Add(new List<Point>()); trackingPoints.ListExitTrack.Add(new List<Point>()); }
        private void ModeChangeAction(object arg) { int mode = (int)arg; CurMode = (mode == CurMode) ? 0 : mode; }
        private void SelectTrackAction(object arg) { SelectedTrack = (int)arg; ReDrawImage(); }
        private void SavePointsAction(object arg)
        {
            string FileName = (string)arg;  

            int NumWriteLine = 2 + (NumTrack << 1);
            string[] textValue = new string[NumWriteLine];
            textValue[0] = ListGCP.Count.ToString() + ' ' + NumTrack.ToString();

            for (int i = 0; i < NumTrack; i++)
                textValue[0] = textValue[0] + ' ' + trackingPoints.ListEnterTrack[i].Count.ToString() + ' ' + trackingPoints.ListExitTrack[i].Count.ToString();

            for (int i = 0; i < ListGCP.Count; i++)
                textValue[1] = textValue[1] + ListGCP[i].X + ' ' + ListGCP[i].Y + ' ' + ListInfoGCP[i].X + ' ' + ListInfoGCP[i].Y + ' ';

            for (int i = 0; i < NumTrack; i++)
            {
                for (int j = 0; j < trackingPoints.ListEnterTrack[i].Count; j++)
                {
                    int idx = ((i + 1) << 1);                    
                    textValue[idx] = textValue[idx] + trackingPoints.ListEnterTrack[i][j].X + ' ' + trackingPoints.ListEnterTrack[i][j].Y + ' ';                    
                }
                for (int j = 0; j < trackingPoints.ListExitTrack[i].Count; j++)
                {
                    int idx = ((i + 1) << 1) + 1;
                    textValue[idx] = textValue[idx] + trackingPoints.ListExitTrack[i][j].X + ' ' + trackingPoints.ListExitTrack[i][j].Y + ' ';
                }
            }            
            System.IO.File.WriteAllLines(FileName, textValue);
        }
        private void LoadPointsAction(object arg)
        {
            string FileName = (string)arg;

            string[] textValue = null;
            textValue = System.IO.File.ReadAllLines(FileName);

            int NumGCP = 0;
            int NumTrackLine = 0;
            int[] NumEnterPoint;
            int[] NumExitPoint;
            { //확인하면서 구현할 것 // 여기부터
                string[] item1 = textValue[0].Split(' ');
                string[] item2 = textValue[1].Split(' ');
                NumGCP = Convert.ToInt32(item1[0]);
                NumTrackLine = Convert.ToInt32(item1[1]);
                NumEnterPoint = new int[NumTrackLine];
                NumExitPoint = new int[NumTrackLine];

                for (int i = 0; i < NumTrackLine; i++)
                {
                    NumEnterPoint[i] = Convert.ToInt32(item1[(i + 1) << 1]);
                    NumExitPoint[i] = Convert.ToInt32(item1[((i + 1) << 1) + 1]);
                }

                List<Point> ListGCP = getListGCP();
                List<Point> ListInfoGCP = getListInfoGCP();

                ListGCP.Clear();
                ListInfoGCP.Clear();

                for (int i = 0; i < NumGCP; i++)
                {
                    double x = Convert.ToDouble(item2[i<<2]);
                    double y = Convert.ToDouble(item2[(i<<2)+1]);
                    double utm_x = Convert.ToDouble(item2[(i<<2)+2]);
                    double utm_y = Convert.ToDouble(item2[(i<<2)+3]);
                    ListGCP.Add(new Point(x, y));
                    ListInfoGCP.Add(new Point(utm_x, utm_y));
                }

                TrackingPoints trackingPoints = GetListTrack();
                trackingPoints.ListEnterTrack.Clear();
                trackingPoints.ListExitTrack.Clear();

                for (int i = 0; i < NumTrackLine; i++)
                {
                    item1 = textValue[(i + 1) << 1].Split(' ');
                    item2 = textValue[((i + 1) << 1) + 1].Split(' ');
                    trackingPoints.ListEnterTrack.Add(new List<Point>());
                    trackingPoints.ListExitTrack.Add(new List<Point>());
                    for (int j = 0; j < NumEnterPoint[i]; j++)
                    {
                        double x = Convert.ToDouble(item1[j << 1]);
                        double y = Convert.ToDouble(item1[(j << 1) + 1]);
                        trackingPoints.ListEnterTrack[i].Add(new Point(x, y));
                    }

                    for (int j = 0; j < NumExitPoint[i]; j++)
                    {
                        double x = Convert.ToDouble(item2[j << 1]);
                        double y = Convert.ToDouble(item2[(j << 1) + 1]);
                        trackingPoints.ListExitTrack[i].Add(new Point(x, y));
                    }
                }
            }
            NumTrack = NumTrackLine;
            ReDrawImage();
        }
        public int GetCurMode() { return CurMode; }
        public int GetNumTrack() { return NumTrack; }
        public int GetSelectedTrack() { return SelectedTrack; }
        public TrackingPoints GetListTrack() { return trackingPoints; } //수정 사항 : structure 구조 생성 및 반환 

        public void AddGCP(double x, double y, double SN, double WE)
        {
            ListGCP.Add(new Point(x, y));
            ListInfoGCP.Add(new Point(SN, WE));
        }
        public int GetCloseGCP(Point pmouse)
        {
            double MinError = double.MaxValue;
            int MinIDX = ListGCP.Count;

            for (int i = 0; i < ListGCP.Count; i++)
            {
                double error = Math.Pow((pmouse.X - ListGCP[i].X), 2.0) + Math.Pow((pmouse.Y - ListGCP[i].Y), 2.0);
                if (MinError > error)
                {
                    MinError = error;
                    MinIDX = i;
                }

                if (MinError < 5.0)
                    return MinIDX;
            }
            if (MinError > 25.0)
                return -1;
            else
                return MinIDX;
        }
        public void DeleteGCP(int idx)
        {
            ListGCP.RemoveAt(idx);
            ListInfoGCP.RemoveAt(idx);
        }
        public void EditLocationGCP(int idx, Point start, Point end)
        {
            Point point = new Point(ListGCP[idx].X + (end.X - start.X), ListGCP[idx].Y + (end.Y - start.Y));
            ListGCP[idx] = point;
        }
        public void EditLocationTrackPoint(int idx, Point start, Point end)
        {
            List<List<Point>> editTrack = idx > 0 ? trackingPoints.ListEnterTrack : trackingPoints.ListExitTrack;
            int AbsIdx = Math.Abs(idx);
            int IdxTrack = (AbsIdx / 100) -1;
            int IdxPoint = AbsIdx % 100;

            Point point = new Point(editTrack[IdxTrack][IdxPoint].X + (end.X - start.X), editTrack[IdxTrack][IdxPoint].Y + (end.Y - start.Y));
            editTrack[IdxTrack][IdxPoint] = point;
        }
        //public Point GetClosePoint(Point pmouse)//ExitTrack에 대해서도 탐색 필요 //index return으로 변경
        public void DeletePoint(int idx)
        {
            List<List<Point>> DelTrack = idx > 0 ? trackingPoints.ListEnterTrack : trackingPoints.ListExitTrack;
            int AbsIdx = Math.Abs(idx);
            int IdxTrack = (AbsIdx / 100) - 1;
            int IdxPoint = AbsIdx % 100;

            DelTrack[IdxTrack].RemoveAt(IdxPoint);
        }
        public int GetClosedPoint(Point pmouse, double width, double height)//ExitTrack에 대해서도 탐색 필요 //index return으로 변경
        {
            double MinError = double.MaxValue;
            int MinTrackIDX = NumTrack;
            int MinIDX = int.MaxValue;
            int iteration = (SelectedTrack == 0) ? NumTrack : 1;
            int cntIDX = 0;

            for (int i = 0; i < iteration; i++)
            {
                int trackIdx = (iteration == 1) ? (SelectedTrack-1) : i;

                for (int j = 0; j < trackingPoints.ListEnterTrack[trackIdx].Count; j++)
                {
                    double diffx = (pmouse.X - trackingPoints.ListEnterTrack[trackIdx][j].X) * width;
                    double diffy = (pmouse.Y - trackingPoints.ListEnterTrack[trackIdx][j].Y) * height;
                    double error = Math.Pow(diffx, 2.0) + Math.Pow(diffy, 2.0);
                    if (MinError > error)
                    {
                        MinError = error;
                        MinTrackIDX = (trackIdx+1);
                        MinIDX = j;
                    }

                    if (MinError < 5.0)
                    {
                        //return new Point((double)MinTrackIDX, (double)MinIDX);
                        return (MinTrackIDX * 100 + j);
                    }
                }
            }

            for (int i = 0; i < iteration; i++)
            {
                int trackIdx = (iteration == 1) ? (SelectedTrack - 1) : i;

                for (int j = 0; j < trackingPoints.ListExitTrack[trackIdx].Count; j++)
                {
                    double diffx = (pmouse.X - trackingPoints.ListExitTrack[trackIdx][j].X) * width;
                    double diffy = (pmouse.Y - trackingPoints.ListExitTrack[trackIdx][j].Y) * height;
                    double error = Math.Pow(diffx, 2.0) + Math.Pow(diffy, 2.0);
                    if (MinError > error)
                    {
                        MinError = error;
                        MinTrackIDX = -(trackIdx+1);
                        MinIDX = -j;
                    }

                    if (MinError < 5.0)
                    {
                        //return new Point((double)MinTrackIDX, (double)MinIDX);

                        return (MinTrackIDX * 100 + MinIDX);
                    }
                }
            }

            if (MinError > 25.0)
            {
                //return new Point(-1.0,-1.0);
                return 0;
            }
            else
            {
                //return new Point((double)MinTrackIDX, (double)MinIDX);
                return (MinTrackIDX * 100 + MinIDX);
            }
        }
        private void DoTrajactory()
        {
            
            int NumFrame = 0;
            int ObjID = 0;
            Point ZeroMV = new Point(0.0, 0.0);
            
            while (true)
            {
                while (!Processing || !AllowTrajactory || QueueDetection.Count == 0 || DrawTrackerList != null) //명령에 의한 Thread 대기 //Main Thread의 정지시 일시대기
                {
                    Thread.Sleep(5);
                    if (TerminateTrajactoryThread)
                        break;
                }
                if (TerminateTrajactoryThread)
                    break;

                BoundingBoxInfo curBBox = ((QueueDetection.Count != 0) ? QueueDetection.Dequeue() : null);

                NumFrame++;
                LastFrameNum = NumFrame;

                ImageProcesser.Remove_Duplicated_BBox(curBBox.getProbability(), curBBox.getBoxID(), curBBox.getBoundingBox());
                
                int idx = TrackerList.Count - 1;
                idx = idx < 0 ? 0 : idx;

                List<double> prob = curBBox.getProbability();
                List<int> boxID = curBBox.getBoxID();
                List<Rect> boxInfo = curBBox.getBoundingBox();

                int NumNew = curBBox.getBoundingBox().Count;
                
                for (int i=0;i< TrackerList.Count;i++) //기존 데이터와 대조 //추후 기존 데이터가 있는 것부터 대조하는걸로 바꾸면 좋을듯
                {//IoU calculation //IoU compare
                    int LastIDX = TrackerList[i].BoxInfo.Count - 1;
                    double threshold = 0.5;
                    Rect PredictedBox = new Rect();

                    double IoUMax = 0.0;
                    int MaxIDX = -1;

                    bool triggerZeroMV = false;
                    int DiffFrameNum = 5;

                    if (LastIDX == 0)
                    {
                        PredictedBox = TrackerList[i].BoxInfo[LastIDX]; //이전 위치가 없으므로 최종 위치 사용
                        threshold = 0.3; //최종 위치를 사용하는 대신 threshold 낮게
                    }
                    else
                    { //GCP 미사용
                        double[,] predX = TrackerList[i].kalman.KalmanPrediction();
                        DiffFrameNum = TrackerList[i].NumFrames[LastIDX] - TrackerList[i].NumFrames[0];
                        Point p1 = TrackerList[i].Center[LastIDX];
                        Point p2 = TrackerList[i].Center[0];
                        int diff = DiffFrameNum;
                        /* //직전 2개 포인트 사용한 MV 측정
                        int PrevIDX = LastIDX - 1;
                        DiffFrameNum = TrackerList[i].NumFrames[LastIDX] - TrackerList[i].NumFrames[PrevIDX];
                        Point p1 = TrackerList[i].Center[LastIDX];
                        Point p2 = TrackerList[i].Center[PrevIDX];
                        int diff = TrackerList[i].NumFrames[LastIDX] - TrackerList[i].NumFrames[PrevIDX];*/

                        Point MV = new Point(((p1.X - p2.X) / (double) diff),((p1.Y - p2.Y)/(double)diff)); //이전 MV 측정
                        if(MV.Equals(ZeroMV))
                            triggerZeroMV = true;
                        diff = NumFrame - TrackerList[i].NumFrames[LastIDX];

                        PredictedBox.X = TrackerList[i].BoxInfo[LastIDX].X + (MV.X * (double)diff); //예측된 위치 계산 (이전 MV 사용한 추정)
                        PredictedBox.Y = TrackerList[i].BoxInfo[LastIDX].Y + (MV.Y * (double)diff);
                        PredictedBox.Width = TrackerList[i].BoxInfo[LastIDX].Width; //IoU 계산시 width, height가 영향을 끼칠 수 있음 //추후 수정?
                        PredictedBox.Height = TrackerList[i].BoxInfo[LastIDX].Height;

                        threshold = 0.5; //최종 위치를 사용하므로 threshold 높게
                    }
                    //if (TrackerList[i].BoxInfo)
                    for(int j=0; j< NumNew;j++)
                    {
                        Rect NewBox = curBBox.getBoundingBox()[j];
                        double iou = ImageProcesser.box_iou(PredictedBox, NewBox);
                        if (IoUMax < iou)
                        {
                            IoUMax = iou;
                            MaxIDX = j;
                        }
                    }
                    if(IoUMax > threshold)
                    {
                        bool AddNewBox = true;
                        Point CurCenter = ImageProcesser.getCenterPoint(curBBox.getBoundingBox()[MaxIDX]);

                        if (LastIDX == 0)
                        {
                            double diffX = CurCenter.X - TrackerList[i].Center[LastIDX].X;
                            double diffY = CurCenter.Y - TrackerList[i].Center[LastIDX].Y;
                            TrackerList[i].kalman.InitKalmanVar(new Point(diffX,diffY));
                        }                            
                        else
                            TrackerList[i].kalman.KalmanUpdate(CurCenter);


                        if (triggerZeroMV || IoUMax > 0.9)
                        {
                            Point p1 = TrackerList[i].Center[LastIDX];
                            Point p2 = ImageProcesser.getCenterPoint(curBBox.getBoundingBox()[MaxIDX]);

                            Point MV = new Point((p1.X - p2.X), (p1.Y - p2.Y));

                            if (MV.Equals(ZeroMV))
                                AddNewBox = false;
                        }

                        if (DiffFrameNum < 5)
                        {
                            AddNewBox = false;
                            TrackerList[i].BoxInfo[LastIDX] = curBBox.getBoundingBox()[MaxIDX];
                            TrackerList[i].Center[LastIDX] = ImageProcesser.getCenterPoint(curBBox.getBoundingBox()[MaxIDX]);
                        }

                        if (AddNewBox)
                        {
                            TrackerList[i].ClassNum.Add(curBBox.getBoxID()[MaxIDX]);
                            TrackerList[i].NumFrames.Add(NumFrame);
                            TrackerList[i].BoxInfo.Add(curBBox.getBoundingBox()[MaxIDX]);
                            TrackerList[i].Center.Add(ImageProcesser.getCenterPoint(curBBox.getBoundingBox()[MaxIDX]));                            
                        }
                        else
                        {
                            TrackerList[i].NumFrames[LastIDX] = NumFrame;
                        }

                        curBBox.getBoundingBox().RemoveAt(MaxIDX);
                        curBBox.getProbability().RemoveAt(MaxIDX);
                        curBBox.getBoxID().RemoveAt(MaxIDX);
                        NumNew--;
                    }
                }

                NumNew = curBBox.getBoundingBox().Count;

                //신규 데이터 모두 추가
                for (int i=0;i< NumNew; i++)
                {
                    if (curBBox.getProbability()[i] < 0.5)
                        continue;

                    TrackingObject NewObject = new TrackingObject();
                    NewObject.ObjID = ObjID++;

                    NewObject.ClassNum = new List<int>();
                    NewObject.ClassNum.Add(curBBox.getBoxID()[i]);

                    NewObject.NumFrames = new List<int> ();
                    NewObject.NumFrames.Add(NumFrame);

                    NewObject.BoxInfo = new List<Rect>();
                    NewObject.BoxInfo.Add(curBBox.getBoundingBox()[i]);

                    NewObject.Center = new List<Point>();
                    NewObject.Center.Add(ImageProcesser.getCenterPoint(curBBox.getBoundingBox()[i]));

                    NewObject.kalman = new LibImageProcessing.KalmanFilter(100.0, 50.0);
                    NewObject.kalman.InitKalmanPos(NewObject.Center[0]);


                    TrackerList.Add(NewObject);
                }

                DrawTrackerList = TrackerList;

                //갱신되지 않는 데이터 리스트에서 삭제 (파일로 저장) //정지 객체에 의한 데이터 과적합 정리
                for (int i = 0; i < TrackerList.Count; i++)
                {
                    int LastIDX = TrackerList[i].BoxInfo.Count - 1;

                    if (NumFrame > TrackerList[i].NumFrames[LastIDX] + 60 )
                    {
                        Point StartPoint = TrackerList[i].Center[0];
                        Point EndPoint = TrackerList[i].Center[LastIDX];
                        Point MiddlePoint = TrackerList[i].Center[LastIDX>>1];

                        #region Checking Enter Point
                        int TrackEnterIDX = -1;
                        int TrackEnterLineIDX = -1;
                        for(int inp = 0; inp < trackingPoints.ListEnterTrack.Count; inp++)
                        {
                            int TrackEnd = trackingPoints.ListEnterTrack[inp].Count - 1;
                            if(ImageProcesser.CheckCrossLine(trackingPoints.ListEnterTrack[inp][0], trackingPoints.ListEnterTrack[inp][TrackEnd], StartPoint, MiddlePoint))
                            {
                                TrackEnterIDX = inp;

                                for(int inl = 1; inl< trackingPoints.ListEnterTrack[inp].Count;inl++)
                                {
                                    if (ImageProcesser.CheckCrossLine(trackingPoints.ListEnterTrack[inp][inl-1], trackingPoints.ListEnterTrack[inp][inl], StartPoint, MiddlePoint))
                                    {
                                        TrackEnterLineIDX = inl-1;
                                        break;
                                    }
                                }
                                break;
                            }
                        }
                        #endregion
                        #region Checking Exit Point
                        int TrackExitIDX = -1;
                        int TrackExitLineIDX = -1;
                        for (int outp = 0; outp < trackingPoints.ListExitTrack.Count; outp++)
                        {
                            int TrackEnd = trackingPoints.ListExitTrack[outp].Count - 1;
                            if (ImageProcesser.CheckCrossLine(trackingPoints.ListExitTrack[outp][0], trackingPoints.ListExitTrack[outp][TrackEnd], MiddlePoint, EndPoint))
                            {
                                TrackExitIDX = outp;

                                for (int outl = 1; outl < trackingPoints.ListExitTrack[outp].Count; outl++)
                                {
                                    if (ImageProcesser.CheckCrossLine(trackingPoints.ListExitTrack[outp][outl - 1], trackingPoints.ListExitTrack[outp][outl], MiddlePoint, EndPoint))
                                    {
                                        TrackExitLineIDX = outl - 1;
                                        break;
                                    }
                                }
                                break;
                            }
                        }
                        #endregion
                        string outputFile = Path.Combine(SaveTrackingObjectFolder, TrackerList[i].ObjID.ToString("D10") + ".txt");//File name : objID

                        string[] textValue = new string[5]; //UTM 없는 경우, 임시
                        textValue[0] = (TrackEnterIDX + 1).ToString() + ' ' + (TrackEnterLineIDX + 1).ToString() + ' ' + (TrackExitIDX + 1).ToString() + ' ' + (TrackExitLineIDX + 1).ToString(); //statistics
                        textValue[1] = TrackerList[i].BoxInfo.Count.ToString();//The number of Points
                        for (int w=0; w<TrackerList[i].BoxInfo.Count; w++)
                        {
                            textValue[2] += TrackerList[i].NumFrames[w].ToString() + ' ';//Frame num
                            textValue[3] += TrackerList[i].Center[w].X.ToString() + ' ';//x point //center
                            textValue[4] += TrackerList[i].Center[w].Y.ToString() + ' ';//y point //center
                        }
                        System.IO.File.WriteAllLines(outputFile, textValue);

                        TrackerList.RemoveAt(i--);
                        continue;
                    }

                    if(LastIDX < 100)
                    {

                    }
                    else if(LastIDX > 100)
                    {
                        if(ImageProcesser.box_iou(TrackerList[i].BoxInfo[0], TrackerList[i].BoxInfo[LastIDX]) > 0.9)
                        {
                            TrackerList[i].BoxInfo.RemoveRange(1, LastIDX);
                            TrackerList[i].Center.RemoveRange(1, LastIDX);
                            TrackerList[i].ClassNum.RemoveRange(1, LastIDX); 
                            TrackerList[i].NumFrames.RemoveRange(1, LastIDX);
                            LastIDX = 0;
                        }
                    }
                    else if(LastIDX > 300)
                    {
                        int numObject = TrackerList[i].BoxInfo.Count;
                        for (int j=1; j < numObject - 1;j++)
                        {
                            if (ImageProcesser.box_iou(TrackerList[i].BoxInfo[j-1], TrackerList[i].BoxInfo[j]) > 0.9)
                            {
                                TrackerList[i].BoxInfo.RemoveAt(j);
                                TrackerList[i].Center.RemoveAt(j);
                                TrackerList[i].ClassNum.RemoveAt(j);
                                TrackerList[i].NumFrames.RemoveAt(j);
                                j--;
                                numObject--;
                                continue;
                            }
                        }
                    }
                }
                //추후 kalman filter
                //추후 histogram compare //When predicted location can not be calculated

                ImgName = curBBox.getImageName();

                //파일명 저장용 queue 생성 및 파일 삭제 추가 (일정 수 이상 쌓였을 때)

                AllowTrajactory = false;
                curBBox = null;
            }
        }
        
        private void DoDecoding()
        {
            while (true)
            {
                while (!Processing || QueueImageName.Count > 5) //명령에 의한 Thread 대기 || Decoding 정보 과적합 회피
                {
                    Thread.Sleep(5);
                    if (TerminateTrajactoryThread)
                        break;
                }
                if (TerminateTrajactoryThread)
                    break;

                //CCTV인 경우 RSTP에서 영상 받아오는 것으로 변경필요
                QueueImageName.Enqueue(VideoDecoder.DecodeNextFrame());
            }
        }

        private void DoDetection()
        {
            while (true)
            {
                while (!Processing || QueueDetection.Count > 5 || QueueImageName.Count == 0) //명령에 의한 Thread 대기 || Detection 정보 과적합 회피 
                {
                    Thread.Sleep(5);
                    if (TerminateTrajactoryThread)
                        break;
                }
                if (TerminateTrajactoryThread)
                    break;

                string OutQueue = QueueImageName.Dequeue();
                QueueDetection.Enqueue(ObjectDetector.YoloDetector(OutQueue));
            }
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
