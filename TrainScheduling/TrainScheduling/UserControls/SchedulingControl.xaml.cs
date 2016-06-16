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
        /// <param name="fileType"></param>
        public void ReadSectionTrainFiles()
        {
            //数据文件路径
            string path = @"..\..\Data";
            var files = Directory.GetFiles(path, "*.txt");
            //获取路径下所有文件信息
            DirectoryInfo folder = new DirectoryInfo(path);
            //将文件名弄出来
            var sectionList = from file in folder.GetFiles("*.txt")
                               where file.Name.ToLower().Contains("section")
                               select file.Name;
            var trainList = from file in folder.GetFiles("*.txt")
                            where file.Name.ToLower().Contains("train")
                            select file.Name;
            //添加到combobox的source中
            SectionComboBox.ItemsSource = sectionList;
            TrainComboBox.ItemsSource = trainList;
        }

    }
}
