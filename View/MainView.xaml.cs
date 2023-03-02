using IotechiCore.DataModel;
using IotechiCore.WPF.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace TrafficObserver.View
{
    /// <summary>
    /// MainView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainView : ViewBaseUserControl<ViewModelBase>
    {
        MainViewModel ViewModel;
        SolidColorBrush[] ColorTrackEnter;
        SolidColorBrush[] ColorTrackExit;
        SolidColorBrush[] ColorCarBoundingBox;
        DialogGCPEdit.MainWindow DialogGCPEdit;
        double[] prevGridSize;
        bool TriggerMove;
        int SavedIDX;
        Point PreviouseMouse;
        int RedrawCnt;
        public MainView() : base(new MainViewModel())
        {
            ViewModel = this.DataContext as MainViewModel;
            InitializeComponent();
            ColorTrackEnter = new SolidColorBrush[] { Brushes.Blue, Brushes.Green, Brushes.Brown };
            ColorTrackExit = new SolidColorBrush[] { Brushes.DarkBlue, Brushes.DarkGreen, Brushes.SaddleBrown };
            ColorCarBoundingBox = new SolidColorBrush[] { Brushes.Yellow, Brushes.Violet, Brushes.Silver, Brushes.Purple };
            prevGridSize = new double[] { MainCanvas.Width, MainCanvas.Height };
            TriggerMove = false;
            SavedIDX = -1;
            PreviouseMouse = new Point(-1.0, -1.0);
            RedrawCnt = 0;
        }

        private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(this);
            Point pmouse = Mouse.GetPosition(this);
            Point dpmouse = new Point(pmouse.X / MainCanvas.ActualWidth, pmouse.Y / MainCanvas.ActualHeight);
            Mouse.Capture(null);

            switch (ViewModel.GetCurMode())
            {
                case (int)ButtonMode.AddGCP://Add GCP
                       //TrafficObserver.MainWindow newWindow = new TrafficObserver.MainWindow();
                       //newWindow.ShowDialog();
                    ViewModel.getListGCP().Add(dpmouse);
                    ReDraw();
                    DialogGCPEdit = new DialogGCPEdit.MainWindow();
                    DialogGCPEdit.ShowDialog();
                    if (DialogGCPEdit.IsCanceled())
                    {
                        ViewModel.getListGCP().RemoveAt(ViewModel.getListGCP().Count - 1);
                        ReDraw();
                    }
                    else
                    {
                        ViewModel.getListInfoGCP().Add(new Point(DialogGCPEdit.GetUTM_WE(), DialogGCPEdit.GetUTM_SN()));
                    }
                    DialogGCPEdit = null;
                    break;
                case (int)ButtonMode.EditGCP://Edit GCP
                    SavedIDX = ViewModel.GetCloseGCP(dpmouse);
                    DialogGCPEdit = new DialogGCPEdit.MainWindow();

                    DialogGCPEdit.SetUTM(ViewModel.getListInfoGCP()[SavedIDX]);
                    DialogGCPEdit.ShowDialog();
                    
                    if (!DialogGCPEdit.IsCanceled())
                        ViewModel.EditInfoGCP(SavedIDX, DialogGCPEdit.GetUTM_WE(), DialogGCPEdit.GetUTM_SN());

                    DialogGCPEdit = null;
                    SavedIDX = -1;
                    break;
                case (int)ButtonMode.MoveGCP:
                    SavedIDX = ViewModel.GetCloseGCP(dpmouse);
                    if(SavedIDX != -1)
                    {
                        PreviouseMouse.X = dpmouse.X;
                        PreviouseMouse.Y = dpmouse.Y;
                        TriggerMove = true;
                        RedrawCnt = 0; 
                    }
                    break;
                case (int)ButtonMode.DeleteGCP:
                    SavedIDX = ViewModel.GetCloseGCP(dpmouse);
                    if (SavedIDX != -1)
                    {
                        ViewModel.DeleteGCP(SavedIDX);
                        SavedIDX = -1;
                    }
                    ReDraw();
                    break;
                case (int)ButtonMode.AddPoint: //Add Point
                case (int)ButtonMode.AddPointExit:
                    int track = ViewModel.GetSelectedTrack();
                    if (track != 0)
                    {                        
                        List<List<Point>> ListTrack = ViewModel.GetCurMode() == (int)ButtonMode.AddPoint ? ViewModel.GetListTrack().ListEnterTrack : ViewModel.GetListTrack().ListExitTrack;
                        ListTrack[track-1].Add(new Point(dpmouse.X, dpmouse.Y));
                        ReDraw();
                    }
                    break;
                case (int)ButtonMode.MovePoint://Move Point
                    SavedIDX = ViewModel.GetClosedPoint(dpmouse, MainCanvas.ActualWidth, MainCanvas.ActualHeight);
                    if (SavedIDX != 0)
                    {
                        PreviouseMouse.X = dpmouse.X;
                        PreviouseMouse.Y = dpmouse.Y;
                        TriggerMove = true;
                        RedrawCnt = 0;
                    }
                    break;
                case (int)ButtonMode.DeletePoint://Delete Point
                    SavedIDX = ViewModel.GetClosedPoint(dpmouse, MainCanvas.ActualWidth, MainCanvas.ActualHeight);
                    if(SavedIDX != 0)
                        ViewModel.DeletePoint(SavedIDX);
                    SavedIDX = -1;
                    ReDraw();
                    break;         
                default:
                    break;
            }
        }
        private void MainCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            //if (TriggerMove)
            {
                Mouse.Capture(this);
                Point pmouse = Mouse.GetPosition(this); 
                Point dpmouse = new Point(pmouse.X / MainCanvas.ActualWidth, pmouse.Y / MainCanvas.ActualHeight);
                Mouse.Capture(null);
                    switch (ViewModel.GetCurMode())
                    {
                        case (int)ButtonMode.MoveGCP:
                            if (SavedIDX != -1)
                            {                                
                                ViewModel.EditLocationGCP(SavedIDX, PreviouseMouse, dpmouse);
                                PreviouseMouse = dpmouse;
                            }
                            ViewModel.ReDrawImage();
                            break;
                        case (int)ButtonMode.MovePoint://Move Point                        
                            if (SavedIDX != 0)
                            {
                                ViewModel.EditLocationTrackPoint(SavedIDX, PreviouseMouse, dpmouse);
                                PreviouseMouse = dpmouse;
                            }
                            ViewModel.ReDrawImage();
                            break;
                        default:
                            break;
                    }
            }
        }
        private int SearchSimilarPoint(List<Point> list, Point point)
        {
            double MinCost = double.MaxValue;
            int MinIDX = list.Count;

            for (int i = 0; i < list.Count; i++)
            {
                double dX = list[i].X - point.X;
                double dY = list[i].Y - point.Y;

                if (Math.Abs(dX) > 5.0 || Math.Abs(dY) > 5.0)
                    continue;
                double cost = Math.Pow(dX, 2.0) + Math.Pow(dY, 2.0);
                if (cost < MinCost)
                {
                    MinCost = cost;
                    MinIDX = i;
                }

                if (MinCost < 8)
                    break;
            }

            return MinIDX;
        }
        private void ReDraw()
        {
            MainCanvas.Children.Clear();

            DrawGCPs();
            DrawTrackPoints(ViewModel.GetSelectedTrack());
            if(ViewModel.DrawTrackerList != null)
            {
                DrawBoundingBoxes();
                DrawTrajactory();
            }
        }
        private void DrawLine(Point src, Point dst, SolidColorBrush color, double thickness)
        {
            Line DrawLine = new Line();
            DrawLine.Stroke = color;
            DrawLine.X1 = src.X * MainCanvas.ActualWidth;
            DrawLine.Y1 = src.Y * MainCanvas.ActualHeight;
            DrawLine.X2 = dst.X * MainCanvas.ActualWidth;
            DrawLine.Y2 = dst.Y * MainCanvas.ActualHeight;
            DrawLine.HorizontalAlignment = HorizontalAlignment.Center;
            DrawLine.VerticalAlignment = VerticalAlignment.Center;
            DrawLine.StrokeThickness = thickness;
            MainCanvas.Children.Add(DrawLine);
        }
        private void DrawPoint(SolidColorBrush color, Point loc, double size)
        {
            double offset = size / 2.0;
            Ellipse DrawPoint = new Ellipse();
            DrawPoint.Stroke = color;
            DrawPoint.StrokeThickness = 1;
            ImageBrush ib = new ImageBrush();
            DrawPoint.Fill = ib;
            DrawPoint.Width = size;
            DrawPoint.Height = size;
            DrawPoint.HorizontalAlignment = HorizontalAlignment.Center;
            DrawPoint.VerticalAlignment = VerticalAlignment.Center;
            Canvas.SetLeft(DrawPoint, (loc.X * MainCanvas.ActualWidth) - offset);
            Canvas.SetTop(DrawPoint, (loc.Y * MainCanvas.ActualHeight) - offset);
            MainCanvas.Children.Add(DrawPoint);
        }
        private void DrawRectangle(SolidColorBrush color, Rect rect)
        {
            Rectangle rectangle = new Rectangle();
            rectangle.Stroke = color;
            rectangle.Width = rect.Width * MainCanvas.ActualWidth;
            rectangle.Height = rect.Height * MainCanvas.ActualHeight;

            Canvas.SetTop(rectangle, rect.Top * MainCanvas.ActualHeight);
            Canvas.SetLeft(rectangle, rect.Left * MainCanvas.ActualWidth);

            MainCanvas.Children.Add(rectangle);
        }
        private void DrawGCPs()
        {            
            List<Point> ListGCP = ViewModel.getListGCP();

            for (int i=0; i < ListGCP.Count; i++)
            {
                DrawPoint(Brushes.Red, ListGCP[i], 10.0);
            }
        }
        private void DrawTrackPoints(int num)
        {            
            if (num == 0)
            {
                for (int l = 0; l < 2; l++)
                {
                    List<List<Point>> ListTrack = (l == 0 ? ViewModel.GetListTrack().ListEnterTrack : ViewModel.GetListTrack().ListExitTrack);
                    SolidColorBrush[] TrackColor = (l ==0 ? ColorTrackEnter : ColorTrackExit);
                    for (int j = 0; j < ListTrack.Count; j++)
                    {
                        for (int i = 0; i < ListTrack[j].Count; i++)
                        {
                            DrawPoint(TrackColor[j], ListTrack[j][i], 10.0);

                            if (i != 0)
                                DrawLine(ListTrack[j][i - 1], ListTrack[j][i], TrackColor[j], 5.0);
                        }
                    }
                }
            }
            else
            {
                for (int l = 0; l < 2; l++)
                {
                    SolidColorBrush[] TrackColor = (l == 0 ? ColorTrackEnter : ColorTrackExit);
                    List<List<Point>> ListTrack = (l == 0 ? ViewModel.GetListTrack().ListEnterTrack : ViewModel.GetListTrack().ListExitTrack);
                    for (int i = 0; i < ListTrack[num - 1].Count; i++)
                    {
                        DrawPoint(TrackColor[num - 1], ListTrack[num - 1][i], 10.0);
                        if (i != 0)
                            DrawLine(ListTrack[num - 1][i - 1], ListTrack[num - 1][i], TrackColor[num - 1], 5.0);
                    }
                }
            }
        }
        private void DrawBoundingBoxes()
        {
            for (int i=0;i< ViewModel.DrawTrackerList.Count;i++)
            {
                int lastIDX = ViewModel.DrawTrackerList[i].BoxInfo.Count - 1;
                if(ViewModel.LastFrameNum == ViewModel.DrawTrackerList[i].NumFrames[lastIDX])
                    DrawRectangle(ColorCarBoundingBox[ViewModel.DrawTrackerList[i].ClassNum[lastIDX]], ViewModel.DrawTrackerList[i].BoxInfo[lastIDX]);
            }
        }
        private void DrawTrajactory()
        {
            for (int i = 0; i < ViewModel.DrawTrackerList.Count; i++)
            {
                for(int j= ViewModel.DrawTrackerList[i].BoxInfo.Count-1; j>0; j--)
                {
                    DrawLine(ViewModel.DrawTrackerList[i].Center[j], ViewModel.DrawTrackerList[i].Center[j - 1], Brushes.LightSkyBlue, 2.0);
                }
            }
            ViewModel.DrawTrackerList = null;
        }
        private void ImageBrush_Changed(object sender, EventArgs e)
        {
            ReDraw();
            ViewModel.AllowTrajactory = true;
        }

        private void GridCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //update GCP
            List<Point> ListGCP = ViewModel.getListGCP();

            for (int i = 0; i < ListGCP.Count; i++)
            {
                ListGCP[i] = new Point((ListGCP[i].X / prevGridSize[0]) * MainCanvas.ActualWidth, (ListGCP[i].Y / prevGridSize[1]) * MainCanvas.ActualHeight);
            }

            //update Track Points
            /*List<List<Point>> ListTrack = ViewModel.GetListTrack().ListEnterTrack;

            for (int j = 0; j < ListTrack.Count; j++)
            {
                for (int i = 0; i < ListTrack[j].Count; i++)
                {
                    ListTrack[j][i] = new Point((ListTrack[j][i].X / prevGridSize[0]) * MainCanvas.ActualWidth, (ListTrack[j][i].Y / prevGridSize[1]) * MainCanvas.ActualHeight);
                }
            }*/

            prevGridSize[0] = MainCanvas.ActualWidth;
            prevGridSize[1] = MainCanvas.ActualHeight;

            ReDraw();
        }

        private void MainCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (TriggerMove)
            {
                TriggerMove = false;
            }
        }

        private void MainCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!TriggerMove)
                e.Handled = true;
            else if(RedrawCnt++ % 4 > 0)
                e.Handled = true;
            else
                e.Handled = false;
        }
    }
}
