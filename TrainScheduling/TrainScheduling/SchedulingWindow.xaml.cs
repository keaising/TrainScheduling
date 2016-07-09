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
using TrainScheduling.Helper;

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
        }

        private void ParameterSettingButton_Click(object sender, RoutedEventArgs e)
        {
            BasicTimetable(); initialized = true;
        }

        bool initialized = false;

        //draw station name, line span et al. 
        private void BasicTimetable()
        {
            //根据grid的划分，合理设计原点，保证时间坐标对齐，stationname 对齐
            GridSchWinTimetable.Children.Clear();
            GridSchWinTimeIndex.Children.Clear();
            GridSchWinStationName.Children.Clear();
            GridSchWinLineName.Children.Clear();
            
           // get H and W of grid
           //double H = 200.0, W = 400.0;
           var rootGrid = GridSchWinTimetable.FindParentGridByName("root");

            // double H = rootGrid.ActualHeight, W = rootGrid.ActualWidth;

            double H = GridSchWinTimetable.ActualHeight; double W = GridSchWinTimetable.ActualWidth;
            //get 分格信息 合理划分 W, 1440 分钟
            int TimeInteval = 120; int NumSta = 3; double RouteLength = 1100; //(in km)
            //number of time lines displayed and the time interval
            int totalFenge = 1440; int _num_time_span = (int)totalFenge / TimeInteval;
            double TimeSpanInUnitMinute = (double)H / 1440, StationSpanInUnitKm = (double)W / RouteLength;

            //origin position; left corner
            //double x_origin = (W / _num_time_line) * 0.5; double y_origin = (H / NumSta) * 0.5;
            double x_origin = 0; double y_origin = 0;// (H / NumSta) * 0.5;
            // CanvasTimetableTimeSpace.
            var myRectangle = new Rectangle();
            myRectangle.StrokeThickness = 1.5;
            myRectangle.Stroke = Brushes.Green;
            myRectangle.Width = W; myRectangle.Height = H;
            myRectangle.HorizontalAlignment = HorizontalAlignment.Left;
            myRectangle.VerticalAlignment = VerticalAlignment.Top;
            //GridSchWinTimetable.Children.Add(myRectangle);

            double timeInterval = (double)(W / _num_time_span); double stationInterval = (double)(H / NumSta);
            //draw time line
            for (int i = 0; i <= _num_time_span; i++)
            {
                var x1 = x_origin + i * timeInterval; var x2 = x_origin + i * timeInterval;
                var y1 = y_origin; var y2 = y_origin + H;
                var myLine = new Line();
                myLine.Stroke = System.Windows.Media.Brushes.Green;
                myLine.X1 = x1;
                myLine.X2 = x2;
                myLine.Y1 = y1;
                myLine.Y2 = y2;
                myLine.HorizontalAlignment = HorizontalAlignment.Left;
                myLine.VerticalAlignment = VerticalAlignment.Top;
                myLine.StrokeThickness = 0.5;
                GridSchWinTimetable.Children.Add(myLine);
            }
            //draw station line
            for (int i = 0; i <= NumSta; i++)
            {
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
                GridSchWinTimetable.Children.Add(mylinestation);
            }

            List<string> strName = new List<string>(); strName.Add("京"); strName.Add("沪"); strName.Add("线");
            ////display railway line name
            for (int i = 0; i < strName.Count; i++)
            {
                RowDefinition rowdef = new RowDefinition();//创建行布局对向
                GridSchWinLineName.RowDefinitions.Add(rowdef);
                TextBlock TextBlockStationName = new TextBlock();
                //get station name
                TextBlockStationName.Text = strName[i];

                //根据车站数目和区间长度调整textblock的高度
                //if(i==0) TextBlockStationName.Height = 100;

                //get font size
                int fontsize = 14; TextBlockStationName.FontSize = fontsize;
                Color color = new Color(); color.R = 155; color.G = 0; color.B = 255; color.A = 255;
                TextBlockStationName.Foreground = new SolidColorBrush(color);
                TextBlockStationName.FontFamily = new FontFamily("Times New Roman");
                TextBlockStationName.VerticalAlignment = VerticalAlignment.Center;
                TextBlockStationName.HorizontalAlignment = HorizontalAlignment.Left;
                Grid.SetRow(TextBlockStationName, i);
                TextBlockStationName.FontStretch = FontStretches.Medium;//100%，紧缩或加宽的程度
                GridSchWinLineName.Children.Add(TextBlockStationName);
            }

            List<string> StaName = new List<string>(); StaName.Add("北京南"); StaName.Add("蚌埠南"); StaName.Add("南京南"); StaName.Add("上海虹桥");
            ////display railway station name
            for (int i = 0; i < StaName.Count; i++)
            {
                RowDefinition rowdef = new RowDefinition();//创建行布局对向
                GridSchWinStationName.RowDefinitions.Add(rowdef);
                TextBlock TextBlockStationName = new TextBlock();
                //get station name
                TextBlockStationName.Text = StaName[i];

                //根据车站数目和区间长度调整textblock的高度
                if (i == 0) TextBlockStationName.Height = y_origin;
                else TextBlockStationName.Height = stationInterval / 2;

                //get font size
                //int fontsize = 12; TextBlockStationName.FontSize = fontsize;
                Color color = new Color(); color.R = 55; color.G = 0; color.B = 255; color.A = 255;
                TextBlockStationName.Foreground = new SolidColorBrush(color);
                TextBlockStationName.FontFamily = new FontFamily("Times New Roman");
                TextBlockStationName.VerticalAlignment = VerticalAlignment.Bottom;
                TextBlockStationName.HorizontalAlignment = HorizontalAlignment.Right;
                Grid.SetRow(TextBlockStationName, i);
                TextBlockStationName.FontStretch = FontStretches.Medium;//100%，紧缩或加宽的程度
                GridSchWinStationName.Children.Add(TextBlockStationName);
            }

            //draw lineIndex
            for (int i = 0; i <= _num_time_span + 1; i++)
            {
                ColumnDefinition coldef = new ColumnDefinition();//创建列布局对向
                GridSchWinTimeIndex.ColumnDefinitions.Add(coldef);
                TextBlock TextBlockaTimeSpanName = new TextBlock();
                //get timespan span
                var timeinterval = 1440.0 / _num_time_span;
                var timespan = W / (_num_time_span + 0.5); var timespan2 = W / _num_time_span;

                var sb = new SolidColorBrush(Colors.Red);
                if (i == 0) TextBlockaTimeSpanName.Width = W / 6;// - timespan/2; //6(n)是画布相对站名部分宽度的比例 6:1，若果布局变化，这里需要调整: w/n - 1/2*(w/(_num_time_line+0.5))
                else TextBlockaTimeSpanName.Width = timespan2;
                if (i > 0)
                {
                    var cur_time = (double)(i - 1) * timeinterval;
                    TimeSpan ts = TimeSpan.FromMinutes(cur_time);
                    var hour = cur_time == 1440 ? "24" : string.Format("{0:hh}", ts);
                    var minute = string.Format("{0:mm}", ts);
                    TextBlockaTimeSpanName.Text = hour + ":" + minute;
                    TextBlockaTimeSpanName.Background = sb;

                    //get font size
                    int fontsize = 12; TextBlockaTimeSpanName.FontSize = fontsize;
                    Color color = new Color(); color.R = 55; color.G = 0; color.B = 255; color.A = 255;
                    TextBlockaTimeSpanName.Foreground = new SolidColorBrush(color);
                    TextBlockaTimeSpanName.FontFamily = new FontFamily("Times New Roman");
                    TextBlockaTimeSpanName.TextAlignment = TextAlignment.Center;
                    TextBlockaTimeSpanName.VerticalAlignment = VerticalAlignment.Center;
                    TextBlockaTimeSpanName.HorizontalAlignment = HorizontalAlignment.Center;
                    Grid.SetColumn(TextBlockaTimeSpanName, i);
                    TextBlockaTimeSpanName.FontStretch = FontStretches.UltraCondensed;//87.5%，紧缩或加宽的程度
                    GridSchWinTimeIndex.Children.Add(TextBlockaTimeSpanName);                    
                }
                else
                {
                    var testRectangle = new Rectangle();
                    testRectangle.StrokeThickness = 1.5;
                    testRectangle.Stroke = Brushes.Green;
                    testRectangle.Width = TextBlockaTimeSpanName.Width; testRectangle.Height = 20;
                    testRectangle.HorizontalAlignment = HorizontalAlignment.Left;
                    testRectangle.VerticalAlignment = VerticalAlignment.Top;                    
                    GridSchWinTimeIndex.Children.Add(testRectangle);
                }                
            }
        }

        private void MetroWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (initialized)
            {
                var testRectangle = new Rectangle();
                testRectangle.StrokeThickness = 1.5;
                testRectangle.Stroke = Brushes.Green;
                testRectangle.Width = 50; testRectangle.Height = 20;
                testRectangle.HorizontalAlignment = HorizontalAlignment.Left;
                testRectangle.VerticalAlignment = VerticalAlignment.Top;
                GridSchWinTimeIndex.Children.Add(testRectangle);

                BasicTimetable();
            }
        }
    }
}
