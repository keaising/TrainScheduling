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
            GridSchWinLineName.RowDefinitions.Clear();
            GridSchWinTimeIndex.ColumnDefinitions.Clear();
            GridSchWinStationName.RowDefinitions.Clear();

            // get H and W of grid          
            var rootGrid = GridSchWinTimetable.FindParentGridByName("root");
            // double H = rootGrid.ActualHeight, W = rootGrid.ActualWidth;
            double H = GridSchWinTimetable.ActualHeight;
            double W = GridSchWinTimetable.ActualWidth;
            //get 分格信息 合理划分 W, 1440 分钟
            int TimeInteval = 120;
            int NumSta = 3;

            //input section length; station name et al.  
            List<string> StaName = new List<string>(); StaName.Add("北京南"); StaName.Add("蚌埠南"); StaName.Add("南京南"); StaName.Add("上海虹桥");
            List<double> SectionLength = new List<double>();
            SectionLength.Add(400); SectionLength.Add(200); SectionLength.Add(300);
            double RouteLength = SectionLength.Sum(); //(in km)

            //number of time lines displayed and the time interval
            int totalFenge = 1440;
            int _num_time_span = (int)totalFenge / TimeInteval;


            double timeInterval = (double)(W / (_num_time_span + 0.5)); //空出右边一部分
            double TimetableWidth = timeInterval * _num_time_span; //timetable所占用宽度
            double stationInterval = (double)(H / (NumSta + 0.4)); //空出上边一部分
            double TimetableHight = stationInterval * NumSta;
            double TimeSpanInUnitMinute = (double)TimetableWidth / 1440.0;
            double StationSpanInUnitKm = (double)TimetableHight / RouteLength;
            //origin position; left corner
            double x_origin = 0;
            double y_origin = 0.4 * stationInterval;// (H / NumSta) * 0.5;

            //draw time line
            for (int i = 0; i <= _num_time_span; i++)
            {
                var x1 = x_origin + i * timeInterval; var x2 = x_origin + i * timeInterval;
                var y1 = y_origin; var y2 = y_origin + H + 0.02 * H; //伸出的做为标识
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
                var a1 = x_origin;
                var a2 = x_origin + TimetableWidth;
                double b1 = 0;
                double b2 = 0;
                if (i == 0)
                {
                    b1 = y_origin;
                    b2 = y_origin;
                }
                else
                {
                    b1 = y_origin + SectionLength.SumFromTo(0, i - 1) * StationSpanInUnitKm;
                    b2 = y_origin + SectionLength.SumFromTo(0, i - 1) * StationSpanInUnitKm;
                }
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

            List<string> strName = new List<string>();
            strName.Add("京");
            strName.Add("沪");
            strName.Add("线");
            ////display railway line name
            for (int i = 0; i < strName.Count; i++)
            {
                RowDefinition rowdef = new RowDefinition();//创建行布局对向
                GridSchWinLineName.RowDefinitions.Add(rowdef);
                TextBlock TextBlockStationName = new TextBlock();
                //get station name
                TextBlockStationName.Text = strName[i];

                //get font size
                int fontsize = 14; TextBlockStationName.FontSize = fontsize;
                Color color = new Color();
                color.R = 155;
                color.G = 0;
                color.B = 255;
                color.A = 255;
                TextBlockStationName.Foreground = new SolidColorBrush(color);
                TextBlockStationName.FontFamily = new FontFamily("Times New Roman");
                TextBlockStationName.VerticalAlignment = VerticalAlignment.Center;
                TextBlockStationName.HorizontalAlignment = HorizontalAlignment.Left;
                Grid.SetRow(TextBlockStationName, i);
                TextBlockStationName.FontStretch = FontStretches.Medium;//100%，紧缩或加宽的程度
                GridSchWinLineName.Children.Add(TextBlockStationName);
            }


            ////display railway station name
            for (int i = 0; i < StaName.Count; i++)
            {
                RowDefinition rowdef = new RowDefinition();//创建行布局对向
                GridSchWinStationName.RowDefinitions.Add(rowdef);
                TextBlock TextBlockStationName = new TextBlock();
                //get station name
                TextBlockStationName.Text = StaName[i];
                TextBlockStationName.Margin = new Thickness(0, 0, 5, 0);

                //根据车站数目和区间长度调整textblock的高度
                if (i == 0)
                    rowdef.Height = new GridLength(y_origin);
                else
                    rowdef.Height = new GridLength(SectionLength[i - 1] * StationSpanInUnitKm);

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

                var testRectangle = new Rectangle();
                testRectangle.StrokeThickness = 1.5;
                testRectangle.Stroke = Brushes.Green;
                testRectangle.Width = 20; testRectangle.Height = y_origin;
                testRectangle.HorizontalAlignment = HorizontalAlignment.Left;
                testRectangle.VerticalAlignment = VerticalAlignment.Top;
                GridSchWinStationName.Children.Add(testRectangle);
            }

            //draw time line Index
            for (int i = 0; i <= _num_time_span + 1; i++)
            {
                ColumnDefinition coldef = new ColumnDefinition();//创建列布局对向
                GridSchWinTimeIndex.ColumnDefinitions.Add(coldef);
                TextBlock TextBlockaTimeSpanName = new TextBlock();
                //get timespan span
                var TimeNumber = 1440.0 / _num_time_span;

                var sb = new SolidColorBrush(Colors.Red);
                if (i == 0)
                {
                    TextBlockaTimeSpanName.Width = W / 6 - timeInterval / 2;
                    coldef.Width = new GridLength(W / 6 - timeInterval / 2);
                } //6(n)是画布相对站名部分宽度的比例 6:1，若果布局变化，这里需要调整: w/n - 1/2*(w/(_num_time_line+0.5))
                else
                {
                    TextBlockaTimeSpanName.Width = timeInterval;
                    coldef.Width = new GridLength(timeInterval);
                }
                if (i > 0)
                {
                    var cur_time = (double)(i - 1) * TimeNumber;
                    TimeSpan ts = TimeSpan.FromMinutes(cur_time);
                    var hour = cur_time == 1440 ? "24" : string.Format("{0:hh}", ts);
                    var minute = string.Format("{0:mm}", ts);
                    TextBlockaTimeSpanName.Text = hour + ":" + minute;
                    //TextBlockaTimeSpanName.Background = sb;

                    //get font size
                    int fontsize = 12; TextBlockaTimeSpanName.FontSize = fontsize;
                    Color color = new Color();
                    color.R = 55;
                    color.G = 0;
                    color.B = 255;
                    color.A = 255;
                    TextBlockaTimeSpanName.Foreground = new SolidColorBrush(color);
                    TextBlockaTimeSpanName.FontFamily = new FontFamily("Times New Roman");
                    TextBlockaTimeSpanName.TextAlignment = TextAlignment.Center;
                    //设置每个textBlock的Margin
                    TextBlockaTimeSpanName.Margin = new Thickness(0, 5, 0, 0);
                    TextBlockaTimeSpanName.VerticalAlignment = VerticalAlignment.Top;
                    TextBlockaTimeSpanName.HorizontalAlignment = HorizontalAlignment.Center;
                    Grid.SetColumn(TextBlockaTimeSpanName, i);
                    TextBlockaTimeSpanName.FontStretch = FontStretches.UltraCondensed;//87.5%，紧缩或加宽的程度
                    GridSchWinTimeIndex.Children.Add(TextBlockaTimeSpanName);
                }
                else
                {
                    // var testRectangle = new Rectangle();
                    //testRectangle.StrokeThickness = 1.5;
                    //testRectangle.Stroke = Brushes.Green;
                    //testRectangle.Width = TextBlockaTimeSpanName.Width; testRectangle.Height = 20;
                    //testRectangle.HorizontalAlignment = HorizontalAlignment.Left;
                    //testRectangle.VerticalAlignment = VerticalAlignment.Top;
                    //GridSchWinTimeIndex.Children.Add(testRectangle);
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
