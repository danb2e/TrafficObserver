using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

namespace DialogGCPEdit
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        private Regex regexNum;
        private Regex regexDot;
        private bool Cancel;

        public double GetUTM_WE() { return Double.Parse(UTM_WE.Text.ToString()); }
        public double GetUTM_SN() { return Double.Parse(UTM_SN.Text.ToString()); }
        public void SetUTM(Point previous)
        {
            UTM_WE.Text = previous.X.ToString();
            UTM_SN.Text = previous.Y.ToString();
        }
        public bool IsCanceled() { return Cancel; }

        void ToolWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Code to remove close box from window
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }
        public MainWindow()
        {
            InitializeComponent();
            Loaded += ToolWindow_Loaded;
            regexNum = new Regex("[^0-9]+");
            regexDot = new Regex("[.]");
            Cancel = false;
        }

        private void Comfirm_Click(object sender, RoutedEventArgs e)
        {
            Cancel = false;
            if (UTM_SN.Text.ToString() == "")
                UTM_SN.Text = "0";
            if(UTM_WE.Text.ToString() == "")
                UTM_WE.Text = "0";
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Cancel = true;
            Close();
        }

        private void PreviewTextBoxInputWE(object sender, TextCompositionEventArgs e)
        {
            if(regexDot.IsMatch(e.Text))
            {
                if (UTM_WE.Text.Contains('.') || UTM_WE.Text.Length < 1)
                    e.Handled = true;
                else
                    e.Handled = false;
            }
            else
                e.Handled = regexNum.IsMatch(e.Text);
        }

        private void PreviewTextBoxInputSN(object sender, TextCompositionEventArgs e)
        {
            if (regexDot.IsMatch(e.Text))
            {
                if (UTM_SN.Text.Contains('.') || UTM_SN.Text.Length < 1)
                    e.Handled = true;
                else
                    e.Handled = false;
            }
            else
                e.Handled = regexNum.IsMatch(e.Text);
        }

    }
}
