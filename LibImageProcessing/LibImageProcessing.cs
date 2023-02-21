using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace LibImageProcessing
{
    public class LibImageProcessing
    {
        private double overlap(double x1, double w1, double x2, double w2)
        {
        
            if (x1 == x2 && w1 == w2)
                return w1;
            //left top point 기준
            double left = x1 > x2 ? x1 : x2;
            double r1 = x1 + w1;
            double r2 = x2 + w2;
            double right = r1 < r2 ? r1 : r2;

            //center point 기준
            /*
            double l1 = x1 - w1 / 2;
            double l2 = x2 - w2 / 2;
            double left = l1 > l2 ? l1 : l2;
            double r1 = x1 + w1 / 2;
            double r2 = x2 + w2 / 2;
            double right = r1 < r2 ? r1 : r2;
            */
            return right - left;
        }
        private double box_intersection(Rect a, Rect b)
        {
            double w = overlap(a.X, a.Width, b.X, b.Width);
            double h = overlap(a.Y, a.Height, b.Y, b.Height);
            if (w < 0 || h < 0) return 0;
            double area = w * h;
            return area;
        }
        private double box_union(Rect a, Rect b)
        {
            double i = box_intersection(a, b);
            double u = a.Width * a.Height + b.Width * b.Height - i;
            return u;
        }
        public int CCW(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            if (((x1 * y2 + x2 * y3 + x3 * y1) - (x2 * y1 + x3 * y2 + x1 * y3)) > 0)
                return 1; //Counter Clockwise
            else
                return -1; //Clockwise or parallel
        }
        public bool CheckCrossLine(Point a, Point b, Point c, Point d)
        {
            double ab = CCW(a.X, a.Y, b.X, b.Y, c.X, c.Y) * CCW(a.X, a.Y, b.X, b.Y, d.X, d.Y);
            double cd = CCW(c.X, c.Y, d.X, d.Y, a.X, a.Y) * CCW(c.X, c.Y, d.X, d.Y, b.X, b.Y);

            return ab <= 0 && cd <= 0;
        }
        public Point getCenterPoint(Rect a)
        {
            Point result = new Point();
            result.X = a.Left + (a.Width / 2.0);
            result.Y = a.Top + (a.Height / 2.0);

            return result;
        }
        public double box_iou(Rect a, Rect b)
        {
            //return box_intersection(a, b)/box_union(a, b);

            double I = box_intersection(a, b);
            double U = box_union(a, b);
            if (I == 0.0 || U == 0.0)
            {
                return 0.0;
            }
            return I / U;
        }
        public int Remove_Duplicated_BBox(List<double> prob, List<int> label, List<Rect> bbox)
        {
            Quick_Sort(prob, label, bbox,0,bbox.Count-1);

            for (int i = 0; i < bbox.Count; i++)
            {
                for (int j = i + 1; j < bbox.Count; j++)
                {
                    if (box_iou(bbox[i], bbox[j]) > 0.8)
                    {
                        prob.RemoveAt(i);
                        label.RemoveAt(i);
                        bbox.RemoveAt(i);
                        j--;
                        continue;
                    }
                }
            }
            return bbox.Count;
        }
        public void Quick_Sort(List<double> prob, List<int> label, List<Rect> bbox, int left, int right)
        {
            if (left < right)
            {
                int pivot = Partition(prob, label, bbox, left, right);

                if (pivot > 1)
                {
                    Quick_Sort(prob, label, bbox, left, pivot - 1);
                }
                if (pivot + 1 < right)
                {
                    Quick_Sort(prob, label, bbox, pivot + 1, right);
                }
            }

        }
        private int Partition(List<double> prob, List<int> label, List<Rect> bbox, int left, int right)
        {
            double pivot = prob[left];
            while (true)
            {

                while (prob[left] < pivot)
                {
                    left++;
                }

                while (prob[right] > pivot)
                {
                    right--;
                }

                if (left < right)
                {
                    if (prob[left] == prob[right]) return right;

                    double prob_temp = prob[left];
                    prob[left] = prob[right];
                    prob[right] = prob_temp;

                    Rect bbox_temp = bbox[left];
                    bbox[left] = bbox[right];
                    bbox[right] = bbox_temp;

                    int label_temp = label[left];
                    label[left] = label[right];
                    label[right] = label_temp;

                }
                else
                {
                    return right;
                }
            }
        }
    }
}
