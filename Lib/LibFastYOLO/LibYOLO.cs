using FastYolo;
using FastYolo.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace LibFastYOLO
{
    public class LibYOLO
    {
        private string cfgFileName;         //Input //YOLO network configuration file
        private string weightFileName;    //Input //YOLO network weight file
        private string classFileName;    //Input //Object naems 
        YoloWrapper Detector;
        public LibYOLO(string cfgname, string weightname, string classname)
        {//"YOLO_config/yolov4_network.cfg", "YOLO_config/yolov4.weights", "YOLO_config/yolov4_obj_names.txt"
            cfgFileName = cfgname;
            weightFileName = weightname;
            classFileName = classname;
            Detector = new YoloWrapper(cfgFileName, weightFileName, classFileName);
        }
        ~LibYOLO()
        {
            if(Detector != null)
                Detector.Dispose();
            Detector = null;
        }
        public void Dispose()
        {
            if (Detector != null)
                Detector.Dispose();
            Detector = null;
        }
        public void YoloSetConfiguration(string cfgname, string weightname, string classname)
        {
            cfgFileName = cfgname;
            weightFileName = weightname;
            classFileName = classname;
            Detector = new YoloWrapper(cfgFileName, weightFileName, classFileName);
        }

        public BoundingBoxInfo YoloDetector(string ImageFileName)
        {
            if (Detector == null)
                return new BoundingBoxInfo();

            IEnumerable<YoloItem> yoloItems = Detector.Detect(ImageFileName);
            FileStream fs = new FileStream(ImageFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            BitmapSource img = BitmapFrame.Create(fs);
            int ImageWidth = img.PixelWidth;
            int ImageHeight = img.PixelHeight;
            fs.Close();
            
            List<YoloItem> tmp = yoloItems.ToList();
            List<int> labels = new List<int>();
            List<double> scores = new List<double>();
            List<Rect> bboxes = new List<Rect>();

            for (int idx = 0; idx < tmp.Count; idx++)
            {
                double prob = tmp[idx].Confidence;
                int tag = Int32.Parse(tmp[idx].Type);
                Rect locate = new Rect((double)tmp[idx].X / (double)img.PixelWidth, (double)tmp[idx].Y / (double)img.PixelHeight, (double)tmp[idx].Width / (double)img.PixelWidth, (double)tmp[idx].Height / (double)img.PixelHeight);

                labels.Add(tag);
                scores.Add(prob);
                bboxes.Add(locate);
            }

            fs = null;
            img = null;
            yoloItems = null;

            return new BoundingBoxInfo(labels, bboxes, scores, ImageFileName);

        }
    }
}
