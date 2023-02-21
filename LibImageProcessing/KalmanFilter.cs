using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LibImageProcessing
{
    public class KalmanFilter
    {
        private double[,] AddMatrix(double[,] a, double[,] b, int multiplier = 1)
        {
            int row = a.GetLength(0);
            int col = a.GetLength(1);
            double[,] result = new double[row, col];
            for (int i = 0; i < row; i++)
            {
                for(int j= 0; j < col; j++)
                {
                    result[i, j] = a[i,j] + (b[i, j] * multiplier);
                }                
            }
            return result;
        }
        static double[,] H      = new double[2, 4] { { 1, 0, 0, 0 },    { 0, 1, 0, 0 }                                  };
        static double[,] transH = new double[4, 2] { { 1, 0 },          { 0, 1 },       { 0, 0 },       { 0, 0 } };
        static double[,] I      = new double[4, 4] { { 1, 0, 0, 0 },    { 0, 1, 0, 0 }, { 0, 0, 1, 0 }, { 0, 0, 0, 1 }  };
        double[,] F;
        double[,] transF;
        double[,] X;    //X_t
        double[,] x;    //Predicted X
        double[,] P;    //P_t
        double[,] transP;
        double[,] p;    //Predicted P
        double[,] R;
        double[,] K;    //Optimal Kalman gain

        public KalmanFilter(double Pinit, double Rinit)
        {
            double dt = 1.0;

            X = new double[4, 1] { { 0 }, { 0 }, { 0 }, { 0 } };
            x = new double[4, 1] { { 0 }, { 0 }, { 0 }, { 0 } };
            P = new double[4, 4] { { Pinit, 0, 0, 0 }, { 0, Pinit, 0, 0 }, { 0, 0, Pinit, 0 }, { 0, 0, 0, Pinit } };
            transP = new double[4, 4] { { Pinit, 0, 0, 0 }, { 0, Pinit, 0, 0 }, { 0, 0, Pinit, 0 }, { 0, 0, 0, Pinit } };
            p = new double[4, 4] { { Pinit, 0, 0, 0 }, { 0, Pinit, 0, 0 }, { 0, 0, Pinit, 0 }, { 0, 0, 0, Pinit } };
            R = new double[2, 2] { { Rinit, 0 }, { 0, Rinit } };
            K = new double[4, 2] { { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 } };
            F = new double[4, 4] { { 1, 0, dt, 0 }, { 0, 1, 0, dt }, { 0, 0, 1, 0 }, { 0, 0, 0, 1 } };
            transF = new double[4, 4] { { 1, 0, 0, 0 }, { 0, 1, 0, 0 }, { dt, 0, 1, 0 }, { 0, dt, 0, 1 } };
        }
        public void InitKalmanPos(Point Position)
        {
            X[0,0] = Position.X;
            X[1,0] = Position.Y;
        }
        public void InitKalmanVar(Point Var)
        {
            X[2, 0] = Var.X;
            X[3, 0] = Var.Y;
        }
        public double[,] KalmanPrediction(double dt = 1.0)
        {
            SetF(dt);
            CalcPredX();
            double[,] result = new double[4, 1] { { 0 }, { 0 }, { 0 }, { 0 } };
            result = x;
            return result;
        }
        private void SetF(double dt)
        {
            F[0, 2] = dt;
            F[1, 3] = dt;
            transF[2, 0] = dt;
            transF[3, 1] = dt;
        }
        private void CalcPredX()
        {
            alglib.rmatrixgemm(4, 1, 4, 1, F, 0, 0, 0, X, 0, 0, 0, 0, ref x, 0, 0); // F * X = Predicted X
        }
        private void CalcPredXwCovar()
        {
            //alglib.rmatrixtranspose(4, 4, P, 0, 0, ref transP, 0, 0); //P^T (transposed P)
            alglib.rmatrixgemm(4, 4, 4, 1, F, 0, 0, 0, P, 0, 0, 0, 0, ref p, 0, 0); // F * P = p (temporal buff)
            double[,] tmp44 = new double[4, 4] { { 0, 0, 0, 0 }, { 0, 0, 0, 0 }, { 0, 0, 0, 0 }, { 0, 0, 0, 0 } };
            alglib.rmatrixgemm(4, 4, 4, 1, p, 0, 0, 0, transF, 0, 0, 0, 0, ref tmp44, 0, 0); //(F * P) * F^T (transposed F) = Predicted P
            p = AddMatrix(tmp44, I); //Add covariance to Predicted P
        }
        private void CalcKalmanGain()
        {
            double[,] tmp42 = new double[4, 2] { { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 } };
            double[,] tmp24 = new double[2, 4] { { 0, 0, 0, 0 }, { 0, 0, 0, 0 } };
            double[,] tmp22 = new double[2, 2] { { 0, 0 }, { 0, 0 } };
            alglib.rmatrixgemm(4, 2, 4, 1, p, 0, 0, 0, transH, 0, 0, 0, 0, ref tmp42, 0, 0);
            alglib.rmatrixgemm(2, 4, 4, 1, H, 0, 0, 0, p, 0, 0, 0, 0, ref tmp24, 0, 0);
            alglib.rmatrixgemm(2, 2, 4, 1, tmp24, 0, 0, 0, transH, 0, 0, 0, 0, ref tmp22, 0, 0);
            tmp22 = AddMatrix(tmp22, R);
            int info;
            alglib.matinvreport rep;
            alglib.rmatrixinverse(ref tmp22, out info, out rep);
            alglib.rmatrixgemm(4, 2, 2, 1, tmp42, 0, 0, 0, tmp22, 0, 0, 0, 0, ref K, 0, 0);
        }
        private void CalcKalmanUpdate(Point point, bool missing)
        {
            double[,] z = new double[2, 1] { { point.X }, { point.Y } };
            double[,] tmp21 = new double[2, 1] { { 0 }, { 0 } };
            double[,] tmp41 = new double[4, 1] { { 0 }, { 0 }, { 0 }, { 0 } };
            double[,] tmp44 = new double[4, 4] { { 0, 0, 0, 0 }, { 0, 0, 0, 0 }, { 0, 0, 0, 0 }, { 0, 0, 0, 0 } };

            alglib.rmatrixgemm(2, 1, 4, 1, H, 0, 0, 0, x, 0, 0, 0, 0, ref tmp21, 0, 0);
            alglib.rmatrixgemm(4, 1, 2, 1, K, 0, 0, 0, AddMatrix(z, tmp21, -1), 0, 0, 0, 0, ref tmp41, 0, 0);
            X = AddMatrix(x, tmp41);

            alglib.rmatrixgemm(4, 4, 2, 1, K, 0, 0, 0, H, 0, 0, 0, 0, ref tmp44, 0, 0);
            alglib.rmatrixgemm(4, 4, 4, 1, tmp44, 0, 0, 0, p, 0, 0, 0, 0, ref P, 0, 0);
            P = AddMatrix(p, P, -1);
        }
        public void KalmanUpdate(Point Position, double dt = 1.0)
        {
            SetF(dt);
            CalcPredX();
            CalcPredXwCovar();
            CalcKalmanGain();
            CalcKalmanUpdate(Position, false);
        }
        public void KalmanMissing()
        {
            CalcPredX();
            CalcPredXwCovar();
            CalcKalmanGain();
            CalcKalmanUpdate(new Point(0,0), true);
        }
    }
}
