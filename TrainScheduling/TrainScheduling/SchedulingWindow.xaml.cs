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
            //GridSchWinTimeSpace.Children.Clear();
            //var H = CanvasTimetableTimeSpace.Height; var W = CanvasTimetableTimeSpace.Width;
            ////origin position; left corner
            //int x_origin = 0; double y_origin = 0;
            //// CanvasTimetableTimeSpace.
            //var myRectangle = new Rectangle();
            //myRectangle.StrokeThickness = 1.5;
            //myRectangle.Stroke = Brushes.Green;
            //myRectangle.Width = W; myRectangle.Height = H;
            //myRectangle.HorizontalAlignment = HorizontalAlignment.Left;
            //myRectangle.VerticalAlignment = VerticalAlignment.Top;
            //GridSchWinTimeSpace.Children.Add(myRectangle);
            
            ////get 分格信息 合理划分 W, 1440 分钟
            //int fenge = 120;
            //int totalFenge = 1440; int _num_time_line = (int)totalFenge / fenge; double timeInterval = W / _num_time_line;
            //double stationInterval = H / _num_time_line;
            //for (int i = 0; i < _num_time_line; i++)
            //{
            //    var x1 = x_origin + i * timeInterval; var x2 = x_origin + i * timeInterval;
            //    var y1 = y_origin; var y2 = y_origin + H;
            //    var myLine = new Line();
            //    myLine.Stroke = System.Windows.Media.Brushes.Green;
            //    myLine.X1 = x1;
            //    myLine.X2 = x2;
            //    myLine.Y1 = y1;
            //    myLine.Y2 = y2;

            //    myLine.Y2 = H;
            //    myLine.HorizontalAlignment = HorizontalAlignment.Left;
            //    myLine.VerticalAlignment = VerticalAlignment.Top;
            //    myLine.StrokeThickness = 0.5;
            //    GridSchWinTimeSpace.Children.Add(myLine);

            //    var a1 = x_origin; var a2 = x_origin + W;
            //    var b1 = y_origin + i * stationInterval; var b2 = y_origin + i * stationInterval;
            //    var mylinestation = new Line();
            //    mylinestation.Stroke = System.Windows.Media.Brushes.Green;
            //    mylinestation.X1 = a1;
            //    mylinestation.X2 = a2;
            //    mylinestation.Y2 = b1;
            //    mylinestation.Y1 = b2;

            //    mylinestation.HorizontalAlignment = HorizontalAlignment.Left;
            //    mylinestation.VerticalAlignment = VerticalAlignment.Top;
            //    mylinestation.StrokeThickness = 0.5;
            //    GridSchWinTimeSpace.Children.Add(mylinestation);
            //}

            //List<TextBlock> LineName = new List<TextBlock>();
            //List<string> strName = new List<string>(); strName.Add("京"); strName.Add("沪"); strName.Add("线");
           
            //TextBlock textBlock1 = new TextBlock();
            //Color color = new Color(); color.R = 255; color.G = 0; color.B = 0; color.A = 255;           
            //textBlock1.Foreground = new SolidColorBrush(color);
            //textBlock1.Text = "text"; textBlock1.Inlines.Add("Hellog");
            //textBlock1.Inlines.Add(new Run("wellllling "));
            //Canvas.SetLeft(textBlock1, 200);
            //Canvas.SetTop(textBlock1, 200);
            ////CanvasTimetableTimeSpace.Children.Add(textBlock1);
            //GridSchWinTimeSpace.Children.Add(textBlock1);

        }

        private TextBlock text(double x1, double y1, string text)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.Text = text;
            //Canvas.SetLeft(textBlock, x1);
            //Canvas.SetTop(textBlock, y1);

            //for (int k = 0; k < 3; k++)
            //{
            //    TextBlock textBlock = text(200, k * 100, strName[k]);
            //    LineName.Add(textBlock);
            //}
            //TextBlock textBlock = text(-200, -100, strName[0]);
            GridSchWinTimeSpace.Children.Add(textBlock);

            //TextBlock textBlock2 = text(300, 300, strName[1]);
            //GridSchWinTimeSpace.Children.Add(textBlock2);

            //foreach (var obj in LineName)
            //    GridSchWinTimeSpace.Children.Add(obj);

            return textBlock;
        }

        private void ParameterSettingButton_Click(object sender, RoutedEventArgs e)
        {
            GridSchWinTimeSpace.Children.Clear();
            var H = CanvasTimetableTimeSpace.Height; var W = CanvasTimetableTimeSpace.Width;
            //origin position; left corner
            int x_origin = 0; double y_origin = 0;
            // CanvasTimetableTimeSpace.
            var myRectangle = new Rectangle();
            myRectangle.StrokeThickness = 1.5;
            myRectangle.Stroke = Brushes.Green;
            myRectangle.Width = W; myRectangle.Height = H;
            myRectangle.HorizontalAlignment = HorizontalAlignment.Left;
            myRectangle.VerticalAlignment = VerticalAlignment.Top;
            GridSchWinTimeSpace.Children.Add(myRectangle);

            //get 分格信息 合理划分 W, 1440 分钟
            int fenge = 120;
            int totalFenge = 1440; int _num_time_line = (int)totalFenge / fenge; double timeInterval = W / _num_time_line;
            double stationInterval = H / _num_time_line;
            for (int i = 0; i < _num_time_line; i++)
            {
                var x1 = x_origin + i * timeInterval; var x2 = x_origin + i * timeInterval;
                var y1 = y_origin; var y2 = y_origin + H;
                var myLine = new Line();
                myLine.Stroke = System.Windows.Media.Brushes.Green;
                myLine.X1 = x1;
                myLine.X2 = x2;
                myLine.Y1 = y1;
                myLine.Y2 = y2;

                myLine.Y2 = H;
                myLine.HorizontalAlignment = HorizontalAlignment.Left;
                myLine.VerticalAlignment = VerticalAlignment.Top;
                myLine.StrokeThickness = 0.5;
                GridSchWinTimeSpace.Children.Add(myLine);

                var a1 = x_origin; var a2 = x_origin + W;
                var b1 = y_origin + i * stationInterval; var b2 = y_origin + i * stationInterval;
                var mylinestation = new Line();
                mylinestation.Stroke = System.Windows.Media.Brushes.Green;
                mylinestation.X1 = a1;
                mylinestation.X2 = a2;
                mylinestation.Y2 = b1;
                mylinestation.Y1 = b2;

                mylinestation.HorizontalAlignment = HorizontalAlignment.Left;
                mylinestation.VerticalAlignment = VerticalAlignment.Top;
                mylinestation.StrokeThickness = 0.5;
                GridSchWinTimeSpace.Children.Add(mylinestation);
            }
            //stack_GridTimetableTimespace.op
        }

        //private void GridSchWinTimeSpace_Initialized(object sender, EventArgs e)
        //{
        //    GridSchWinTimeSpace.Children.Clear();
        //    var H =  CanvasTimetableTimeSpace.Height; var W = CanvasTimetableTimeSpace.Width;
        //    //origin position; left corner
        //    int x_origin = 0; double y_origin = 0;
        //    // CanvasTimetableTimeSpace.
        //    var myRectangle = new Rectangle();
        //    myRectangle.StrokeThickness = 1.5;
        //    myRectangle.Stroke = Brushes.Green;
        //    myRectangle.Width = W; myRectangle.Height = H;
        //    myRectangle.HorizontalAlignment = HorizontalAlignment.Left;
        //    myRectangle.VerticalAlignment = VerticalAlignment.Top;
        //    GridSchWinTimeSpace.Children.Add(myRectangle);

        //    //get 分格信息 合理划分 W, 1440 分钟
        //    int fenge = 120;
        //    int totalFenge = 1440; int _num_time_line = (int)totalFenge / fenge; double timeInterval = W / _num_time_line;
        //    double stationInterval = H / _num_time_line;
        //    for (int i = 0; i < _num_time_line; i++)
        //    {
        //        var x1 = x_origin + i * timeInterval; var x2 = x_origin + i * timeInterval;
        //        var y1 = y_origin; var y2 = y_origin + H;
        //        var myLine = new Line();
        //        myLine.Stroke = System.Windows.Media.Brushes.Green;
        //        myLine.X1 = x1;
        //        myLine.X2 = x2;
        //        myLine.Y1 = y1;
        //        myLine.Y2 = y2;

        //        myLine.Y2 = H;
        //        myLine.HorizontalAlignment = HorizontalAlignment.Left;
        //        myLine.VerticalAlignment = VerticalAlignment.Top;
        //        myLine.StrokeThickness = 0.5;
        //        GridSchWinTimeSpace.Children.Add(myLine);

        //        var a1 = x_origin; var a2 = x_origin + W;
        //        var b1 = y_origin + i * stationInterval; var b2 = y_origin + i * stationInterval;
        //        var mylinestation = new Line();
        //        mylinestation.Stroke = System.Windows.Media.Brushes.Green;
        //        mylinestation.X1 = a1;
        //        mylinestation.X2 = a2;
        //        mylinestation.Y2 = b1;
        //        mylinestation.Y1 = b2;

        //        mylinestation.HorizontalAlignment = HorizontalAlignment.Left;
        //        mylinestation.VerticalAlignment = VerticalAlignment.Top;
        //        mylinestation.StrokeThickness = 0.5;
        //        GridSchWinTimeSpace.Children.Add(mylinestation);
        //    }
        //}


    }
}
