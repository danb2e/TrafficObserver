using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LibFastYOLO
{
    public class BoundingBoxInfo
    {
        private List<int> BoxID;
        private List<Rect> BoundingBox;
        private List<double> Probability;
        private string ImageName;
        public BoundingBoxInfo( List<int> id, List<Rect> box, List<double> prob, string imageName)
        {
            BoxID = id;
            BoundingBox = box;
            Probability = prob;
            ImageName = imageName;
        }
        public BoundingBoxInfo()
        {
            ImageName = "";
        }
        public List<int> getBoxID() { return BoxID; }
        public int getBoxID(int idx) { return BoxID[idx]; }
        public void setBoxID(List<int> boxID) { BoxID = boxID; }
        public List<Rect> getBoundingBox() { return BoundingBox; }
        public Rect getBoundingBox(int idx) { return BoundingBox[idx]; }
        public void setBoundingBox(List<Rect> box) { BoundingBox = box; }
        public List<double> getProbability() { return Probability; }
        public double getProbability(int idx) { return Probability[idx]; }
        public void setProbability(List<double> prob) { Probability = prob; }
        public string getImageName() { return ImageName; }
        public void setImageName(string imageName) { ImageName = imageName; }

    }
}
