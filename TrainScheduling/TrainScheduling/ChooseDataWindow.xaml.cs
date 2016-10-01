using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using TrainScheduling.Helper;

namespace TrainScheduling
{
    /// <summary>
    /// Interaction logic for ChooseDataWindow.xaml
    /// </summary>
    public partial class ChooseDataWindow : Window
    {
        public ChooseDataWindow()
        {
            InitializeComponent();
            Files = GetFiles();
            InitComboBox();
        }

        private List<FileInfo> Files;


        public List<FileInfo> GetFiles()
        {
            var path = @"..\..\Data";
            DirectoryInfo theFolder = new DirectoryInfo(path);
            return theFolder.GetFiles().ToList();
        }
        public Dictionary<String, List<FileInfo>> GetFilesInGroup(List<FileInfo> files)
        {
            var partialNames = new List<String> { "train", "station", "section" };
            var filesInGroup = new Dictionary<String, List<FileInfo>>();
            partialNames.ForEach(e => filesInGroup.Add(e, new List<FileInfo>()));
            files.ForEach(fileInfo =>
            {
                partialNames.ForEach(name =>
                {
                    if (fileInfo.Name.Contains(name))
                    {
                        filesInGroup[name].Add(fileInfo);
                    }
                });
            });
            return filesInGroup;
        }

        public void InitComboBox()
        {
            var filesInGroup = GetFilesInGroup(Files);
            var type = TrainComboBox.GetType();
            var comboxes = new List<ComboBox> { TrainComboBox, StationComboBox, SectionComboBox };
            comboxes.ForEach(com =>
            {
                filesInGroup.Keys.ToList().ForEach(group =>
                {
                    if (com.Name.ToLower().Contains(group))
                    {
                        filesInGroup[group].ForEach(file =>
                        {
                            com.Items.Add(file.Name);                            
                        });
                    }
                });
            });
        }

        private void OKBtn_Click(object sender, RoutedEventArgs e)
        {
            var train = Files.FindLast(f => f.Name == TrainComboBox.SelectedItem.ToString());
            var station = Files.FindLast(f => f.Name == StationComboBox.SelectedItem.ToString());
            var section = Files.FindLast(f => f.Name == SectionComboBox.SelectedItem.ToString());
            BaseDataModel.List = new List<string> { train.FullName, station.FullName, section.FullName };
            this.Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
