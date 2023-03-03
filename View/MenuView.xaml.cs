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
    /// MenuView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MenuView : ViewBaseUserControl<ViewModelBase>
    {
        public MenuView() : base(new MenuViewModel())
        {
            InitializeComponent();
        }

        private void TopDown(object sender, MouseButtonEventArgs e)
        {
            if(TopU.Visibility == Visibility.Visible)
            {
                TopU.Visibility = Visibility.Hidden;
            }
            else
            {
                TopU.Visibility = Visibility.Visible;
            }
        }
    }
}
