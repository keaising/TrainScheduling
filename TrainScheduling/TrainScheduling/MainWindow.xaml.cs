using MahApps.Metro.Controls;
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
using TrainScheduling.UserControls;

namespace TrainScheduling
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();

           
        }
       
    }

    public enum SexOpt { Male, Female };

    public class Member
    {
        public string Name { get; set; }
        public string Age { get; set; }
        public SexOpt Sex { get; set; }
        public bool Pass { get; set; }
        public Uri Email { get; set; }
    }
}
