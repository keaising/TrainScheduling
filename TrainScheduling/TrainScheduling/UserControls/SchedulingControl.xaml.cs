using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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

namespace TrainScheduling.UserControls
{
    /// <summary>
    /// Interaction logic for SchedulingControl.xaml
    /// </summary>
    public partial class SchedulingControl : UserControl
    {
        public SchedulingControl()
        {
            InitializeComponent();
            ReadSectionTrainFiles();
        }
        /// <summary>
        /// 读取Section和Train的文件名
        /// </summary>
        public void ReadSectionTrainFiles()
        {
            string path = @"..\..\Data";
            var files = Directory.GetFiles(path, "*.txt");
            ObservableCollection<string> SectionItems = new ObservableCollection<string>();
            DirectoryInfo folder = new DirectoryInfo(path);

            foreach (FileInfo file in folder.GetFiles("*.txt"))
            {
                SectionItems.Add(file.Name);
            }
            SectionComboBox.ItemsSource = SectionItems;
        }

    }
}
