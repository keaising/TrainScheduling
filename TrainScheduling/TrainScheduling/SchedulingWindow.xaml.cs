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
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using System.Globalization;

namespace TrainScheduling
{
    /// <summary>
    /// Interaction logic for SchedulingWindow.xaml
    /// </summary>
    public partial class SchedulingWindow : MetroWindow
    {
        public SchedulingWindow()
        {
            InitializeComponent();
            test(ref a);
            BiggestDelayTimeTextbox.Text = a.ToString();
        }

        int a = 0;

        void test(ref int b)
        {
            b = 2;
        }

        private void CanvasTimetableTimeSpace_Initialized(object sender, EventArgs e)
        {
            GridSchWinTimeSpace.Children.Clear();
            var H = CanvasTimetableTimeSpace.Height; var W = CanvasTimetableTimeSpace.Width;
            //origin position; left corner
            int x_origin = 20; int y_origin = 0;
            // CanvasTimetableTimeSpace.
            var myRectangle = new Rectangle();
            myRectangle.StrokeThickness = 1.5;
            myRectangle.Stroke = Brushes.Green;
            myRectangle.Width = W; myRectangle.Height = H;
            GridSchWinTimeSpace.Children.Add(myRectangle);

            //get 分格信息 合理划分 W, 1440 分钟
            int fenge = 30;
            int totalFenge = 1440; int _num_time_line = (int)totalFenge / fenge; double timeInterval = W / _num_time_line;
            for (int i = 0; i < _num_time_line; i++)
            {
                var x1 = x_origin + i * timeInterval; var x2 = x_origin + i * timeInterval;
                var y1 = y_origin; var y2 = H;
                var myLine = new Line();
                myLine.Stroke = System.Windows.Media.Brushes.Green;
                myLine.X1 = x1;
                myLine.X2 = x2;
                myLine.Y1 = y1;
                myLine.Y2 = y2;
                myLine.HorizontalAlignment = HorizontalAlignment.Left;
                myLine.VerticalAlignment = VerticalAlignment.Center;
                myLine.StrokeThickness = 0.5;
                GridSchWinTimeSpace.Children.Add(myLine);
            }

            List<TextBlock> LineName = new List<TextBlock>();
            List<string> strName = new List<string>(); strName.Add("京"); strName.Add("沪"); strName.Add("线");
            //for (int k = 0; k < 3; k++)
            //{
            //    TextBlock textBlock = text(200, k * 100, strName[k]);
            //    LineName.Add(textBlock);
            //}
            TextBlock textBlock = text(-200, -100, strName[0]);
            GridSchWinTimeSpace.Children.Add(textBlock);

            //TextBlock textBlock2 = text(300, 300, strName[1]);
            //GridSchWinTimeSpace.Children.Add(textBlock2);

            //foreach (var obj in LineName)
            //    GridSchWinTimeSpace.Children.Add(obj);

        }

        private TextBlock text(double x1, double y1, string text)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.Text = text;
            Canvas.SetLeft(textBlock, x1);
            Canvas.SetTop(textBlock, y1);
            
            return textBlock;
        }
    }
}
