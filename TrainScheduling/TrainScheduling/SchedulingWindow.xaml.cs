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
using TrainScheduling.Model;
using TrainScheduling.Algorithm;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;

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

        List<Ctrain> gtrain = new List<Ctrain>();
        List<Crailway_station> gstation = new List<Crailway_station>();
        List<Crailway_section> gsection = new List<Crailway_section>();
        bool initialized = false;
        bool BoolInputData = false;
        bool RunTSTA = false;
        //button_click input data，这里输入data的方式后面需要改动
        private void ParameterSettingButton_Click(object sender, RoutedEventArgs e)
        {
            if (!BoolInputData)
            {
                InputData(gtrain, gstation, gsection);
                MessageBox.Show("基础数据读取成功！");
            }
        }

        /// <summary>
        /// button_click 画底图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DrawBasicPanel_Click(object sender, RoutedEventArgs e)
        {
            if (!initialized)
                BasicTimetable(gstation, gsection); initialized = true;
        }


        /// <summary>
        /// button_click 运行TSTA算法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Alg_TSTA_Click(object sender, RoutedEventArgs e)
        {
            if (gtrain.Count == 0 || gsection.Count == 0 || gstation.Count == 0)
                MessageBox.Show("未输入基础数据，请选择数据！");
            else
            {
                if (RunTSTA)
                    MessageBox.Show("不要调皮，你已经运行过TSTA算法咯！");
                else
                {
                    CTATS railway_sys = new CTATS(gtrain, gstation, gsection, 0);
                    MessageBox.Show("TSTA算法运行成功！");
                    RunTSTA = true;
                }
            }
        }

        /// <summary>
        /// button_click 画运行图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DrawTimetable_Click(object sender, RoutedEventArgs e)
        {
            if (gtrain.Count == 0 || gsection.Count == 0 || gstation.Count == 0)
                MessageBox.Show("未输入基础数据，请选择数据！");
            else
            {
                if (!RunTSTA)
                    MessageBox.Show("未运行任何调度算法，请先运行调度/调整算法！");
                else if (RunTSTA)
                {
                    DisplayTrainTimeTable(gtrain, 120, gsection);
                }
            }
        }

        /// <summary>
        /// 画底图
        /// </summary>
        /// <param name="inputstation"></param>
        /// <param name="inputsection"></param>
        private void BasicTimetable(List<Crailway_station> inputstation, List<Crailway_section> inputsection)
        {
            if (inputsection.Count == 0 || inputsection.Count == 0)
                MessageBox.Show("未输入基础数据，请选择数据！");
            else
            {
                //根据grid的划分，合理设计原点，保证时间坐标对齐，stationname 对齐
                //现在以读数据的方式将线路数据读进去，以后需要进一步考虑“点选方案”
                GridSchWinTimetable.Children.Clear();
                GridSchWinTimeIndex.Children.Clear();
                GridSchWinStationName.Children.Clear();
                GridSchWinLineName.Children.Clear();
                GridSchWinLineName.RowDefinitions.Clear();
                GridSchWinTimeIndex.ColumnDefinitions.Clear();
                GridSchWinStationName.RowDefinitions.Clear();

                List<Crailway_station> station = new List<Crailway_station>();
                List<Crailway_section> section = new List<Crailway_section>();
                //InputData(train, station, section);
                foreach (var obj in inputstation)
                    station.Add(obj.Clone());
                foreach (var obj in inputsection)
                    section.Add(obj.Clone());

                //input section length; station name et al.  
                List<string> StaName = new List<string>();
                List<double> SectionLength = new List<double>();
                List<double> TimetableSectionLength = new List<double>();

                for (int j = 0; j < station.Count; j++)
                    StaName.Add("Station_" + station[station.Count - 1 - j].stationID.ToString());
                for (int j = 0; j < section.Count; j++)
                {
                    SectionLength.Add(section[section.Count - 1 - j].length);
                    TimetableSectionLength.Add(section[j].length);
                }

                //StaName.Add("北京南"); StaName.Add("蚌埠南"); StaName.Add("南京南"); StaName.Add("上海虹桥");           
                //SectionLength.Add(400); SectionLength.Add(200); SectionLength.Add(300);
                double RouteLength = SectionLength.Sum(); //(in km)

                //// get H and W of grid          
                //var rootGrid = GridSchWinTimetable.FindParentGridByName("root");
                //// double H = rootGrid.ActualHeight, W = rootGrid.ActualWidth;
                double H = GridSchWinTimetable.ActualHeight;
                double W = GridSchWinTimetable.ActualWidth;
                //get 分格信息 合理划分 W, 1440 分钟
                int GivenFengeInUnitTimeSpan = 120;
                int NumSection = StaName.Count - 1;

                double[] GTP = new double[8];
                GTP = GridTimeTableParameter(GridSchWinTimetable, GivenFengeInUnitTimeSpan, SectionLength);
                double x_origin = GTP[0];
                double y_origin = GTP[1];
                double TimetableWidth = GTP[2];
                double TimetableHight = GTP[3];
                double TimeSpanInUnitMinute = GTP[4];
                double StationSpanInUnitKm = GTP[5];
                double TimeInterval = GTP[6];
                double _num_time_span = GTP[7];

                //draw time line
                for (int i = 0; i <= _num_time_span; i++)
                {
                    var x1 = x_origin + i * TimeInterval; var x2 = x_origin + i * TimeInterval;
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
                for (int i = 0; i <= NumSection; i++)
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
                    var color = new System.Windows.Media.Color();
                    color.R = 155;
                    color.G = 0;
                    color.B = 255;
                    color.A = 255;
                    TextBlockStationName.Foreground = new SolidColorBrush(color);
                    TextBlockStationName.FontFamily = new System.Windows.Media.FontFamily("Times New Roman");
                    TextBlockStationName.VerticalAlignment = VerticalAlignment.Center;
                    TextBlockStationName.HorizontalAlignment = HorizontalAlignment.Left;
                    Grid.SetRow(TextBlockStationName, i);
                    TextBlockStationName.FontStretch = FontStretches.Medium;//100%，紧缩或加宽的程度
                    GridSchWinLineName.Children.Add(TextBlockStationName);
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
                        TextBlockaTimeSpanName.Width = W / 6 - TimeInterval / 2;
                        coldef.Width = new GridLength(W / 6 - TimeInterval / 2);
                    } //6(n)是画布相对站名部分宽度的比例 6:1，若果布局变化，这里需要调整: w/n - 1/2*(w/(_num_time_line+0.5))
                    else
                    {
                        TextBlockaTimeSpanName.Width = TimeInterval;
                        coldef.Width = new GridLength(TimeInterval);
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
                        var color = new System.Windows.Media.Color();
                        color.R = 55;
                        color.G = 0;
                        color.B = 255;
                        color.A = 255;
                        TextBlockaTimeSpanName.Foreground = new SolidColorBrush(color);
                        TextBlockaTimeSpanName.FontFamily = new System.Windows.Media.FontFamily("Times New Roman");
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
                    var color = new System.Windows.Media.Color(); color.R = 55; color.G = 0; color.B = 255; color.A = 255;
                    TextBlockStationName.Foreground = new SolidColorBrush(color);
                    TextBlockStationName.FontFamily = new System.Windows.Media.FontFamily("Times New Roman");
                    TextBlockStationName.VerticalAlignment = VerticalAlignment.Bottom;
                    TextBlockStationName.HorizontalAlignment = HorizontalAlignment.Right;
                    Grid.SetRow(TextBlockStationName, i);
                    TextBlockStationName.FontStretch = FontStretches.Medium;//100%，紧缩或加宽的程度
                    GridSchWinStationName.Children.Add(TextBlockStationName);
                }
            }
        }

        /// <summary>
        /// 拖动窗口大小之后重绘
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MetroWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var t1 = DateTime.Now;
            if (initialized)
            {
                var testRectangle = new System.Windows.Shapes.Rectangle();
                testRectangle.StrokeThickness = 1.5;
                testRectangle.Stroke = System.Windows.Media.Brushes.Green;
                testRectangle.Width = 50; testRectangle.Height = 20;
                testRectangle.HorizontalAlignment = HorizontalAlignment.Left;
                testRectangle.VerticalAlignment = VerticalAlignment.Top;
                GridSchWinTimeIndex.Children.Add(testRectangle);

                BasicTimetable(gstation, gsection);
            }

            if (RunTSTA)
                DisplayTrainTimeTable(gtrain, 120, gsection);
            var t2 = DateTime.Now;
            var ts = t2 - t1;

            if (ts.Milliseconds != 0)
            {
                float per = 1000 / ts.Milliseconds;
                FpsLabel.Content = per.ToString() + "帧/s";
            }

            #region 双缓冲绘图
            ////1、在内存中建立一块“虚拟画布”  
            //Bitmap bmp = new Bitmap(200, 200);

            ////2、获取这块内存画布的Graphics引用  
            //Graphics bufferGraphics = Graphics.FromImage(bmp);

            ////3、在这块内存画布上绘图  
            //bufferGraphics.Clear(System.Drawing.Color.White);
            //bufferGraphics.DrawRectangle(Pens.Black, 0, 0, bmp.Width - 1, bmp.Height - 1);
            //bufferGraphics.DrawEllipse(Pens.Red, 10, 10, 100, 50);
            //bufferGraphics.DrawLine(Pens.Green, 10, 100, 100, 200);

            ////4、将内存画布画到窗口中  
            //using (Graphics g = e.Graphics)
            //{
            //    g.DrawImage(bmp, 10, 10);
            //}

            ////5. 释放资源  
            //bmp.Dispose();
            //bufferGraphics.Dispose();
            #endregion

        }

        /// <summary>
        /// 画运行图
        /// </summary>
        /// <param name="train"></param>
        /// <param name="GivenFengeInUnitTimeSpan"></param>
        /// <param name="Section"></param>
        private void DisplayTrainTimeTable(List<Ctrain> train, int GivenFengeInUnitTimeSpan, List<Crailway_section> Section)
        {
            List<double> SectionLength = new List<double>();
            for (int j = 0; j < Section.Count; j++)
                SectionLength.Add(Section[j].length);

            int TimeMaxIndex = 0;
            foreach (Ctrain obj in train)
                TimeMaxIndex = Math.Max(TimeMaxIndex, obj.departure[obj.route[obj.departure.Count() - 1]]);

            double[] GTP = new double[8];
            GTP = GridTimeTableParameter(GridSchWinTimetable, GivenFengeInUnitTimeSpan, SectionLength);
            double x_origin = GTP[0];
            double y_origin = GTP[1];
            double TimetableWidth = GTP[2];
            double TimetableHight = GTP[3];
            double TimeSpanInUnitMinute = GTP[4];
            double StationSpanInUnitKm = GTP[5];
            double TimeInterval = GTP[6];
            double _num_time_span = GTP[7];
            double H = GridSchWinTimetable.ActualHeight;

            List<double> TimetableSectionLength = new List<double>();
            double railwaylength = 0.0; TimetableSectionLength.Add(railwaylength);
            for (int j = 0; j < SectionLength.Count; j++)
            {
                railwaylength = railwaylength + SectionLength[j];
                TimetableSectionLength.Add(railwaylength);
            }

            //列车图
            for (int i = 0; i < train.Count(); i++)
            {
                //不用分上下行绘图, 但是需要进一步考虑如果列车的途径站点并不存在与当前路径上的情况
                for (int j = 0; j < train[i].arrival.Count() - 1; j++)
                {
                    //plot dwelling time line
                    var DwellingTimeLine = new Line();
                    DwellingTimeLine.Stroke = System.Windows.Media.Brushes.DarkBlue;
                    DwellingTimeLine.StrokeThickness = 1;
                    double dx1 = TimeSpanInUnitMinute * train[i].arrival[train[i].route[j]] / 60 + x_origin; //+号前面一部分是为了将arrival timed转化为分钟制
                    double dx2 = TimeSpanInUnitMinute * train[i].departure[train[i].route[j]] / 60 + x_origin;
                    double dy1 = 0.0, dy2 = 0.0;
                    dy1 = H - StationSpanInUnitKm * TimetableSectionLength[train[i].route[j]];
                    dy2 = dy1;
                    DwellingTimeLine.X1 = dx1;
                    DwellingTimeLine.X2 = dx2;
                    DwellingTimeLine.Y1 = dy1;
                    DwellingTimeLine.Y2 = dy2;
                    GridSchWinTimetable.Children.Add(DwellingTimeLine);

                    //plot travelling time line
                    var TravellingTimeLine = new Line();
                    TravellingTimeLine.Stroke = System.Windows.Media.Brushes.DarkBlue;
                    TravellingTimeLine.StrokeThickness = 1;
                    double tx1 = dx2; double ty1 = dy2;
                    double tx2 = 0.0; double ty2 = 0.0;
                    tx2 = TimeSpanInUnitMinute * train[i].arrival[train[i].route[j + 1]] / 60 + x_origin;
                    ty2 = H - StationSpanInUnitKm * TimetableSectionLength[train[i].route[j + 1]];
                    TravellingTimeLine.X1 = tx1;
                    TravellingTimeLine.X2 = tx2;
                    TravellingTimeLine.Y1 = ty1;
                    TravellingTimeLine.Y2 = ty2;
                    GridSchWinTimetable.Children.Add(TravellingTimeLine);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mygrid"></param>
        /// <param name="FengeInUnitTimeSpan"></param>
        /// <param name="SectionLength"></param>
        /// <returns></returns>
        private double[] GridTimeTableParameter(Grid mygrid, int FengeInUnitTimeSpan, List<double> SectionLength)
        {
            //0-x_origin; 1-y_origin; 2-TimeTableWidth
            double[] GTP = new double[8];

            //input section length; station name et al. 
            double RouteLength = SectionLength.Sum(); //(in km)
            // get H and W of grid
            double H = mygrid.ActualHeight;
            double W = mygrid.ActualWidth;
            //get 分格信息 合理划分 W, 1440 分钟          
            int NumSection = SectionLength.Count();
            //number of time lines displayed and the time interval
            int TotalFenge = 1440;
            //number of timespan
            int _num_time_span = (int)TotalFenge / FengeInUnitTimeSpan;
            double TimeInterval = (double)(W / (_num_time_span + 0.5)); //空出右边一部分，TimeInterval为timetable中一个timespan在time维度上所占用的长度
            double TimetableWidth = TimeInterval * _num_time_span; //timetable所占用宽度
            double StationInterval = (double)(H / (NumSection + 0.4)); //空出上边一部分
            double TimetableHight = StationInterval * NumSection;
            double TimeSpanInUnitMinute = (double)TimetableWidth / 1440.0;
            double StationSpanInUnitKm = (double)TimetableHight / RouteLength;
            //origin position; left corner
            double x_origin = 0;
            double y_origin = 0.4 * StationInterval;

            GTP[0] = x_origin;
            GTP[1] = y_origin;
            GTP[2] = TimetableWidth;
            GTP[3] = TimetableHight;
            GTP[4] = TimeSpanInUnitMinute;
            GTP[5] = StationSpanInUnitKm;
            GTP[6] = TimeInterval;
            GTP[7] = _num_time_span;

            return GTP;
        }

        /// <summary>
        /// 导入数据
        /// </summary>
        /// <param name="train"></param>
        /// <param name="station"></param>
        /// <param name="section"></param>
        private void InputData(List<Ctrain> train, List<Crailway_station> station, List<Crailway_section> section)
        {
            CParameter parameter = new CParameter();
            int _ntrain = parameter.Dstation_data_input_nbtrain;
            Coutput_Experiment_Statistic_Result_Stream out_stream = new Coutput_Experiment_Statistic_Result_Stream();
            StreamWriter Dtrain_output = out_stream.DTrains_statistic_output("TATS");
            StreamWriter Dstation_output = out_stream.DStation_statistic_output("TATS");
            StreamReader[] input_reader = out_stream.input_data_streamwriter(_ntrain, 0);
            CRead_Inputdata ReadData = new CRead_Inputdata();
            ReadData.input_train_data(input_reader[0], train);
            ReadData.input_station_data(input_reader[1], station);
            ReadData.input_section_data(input_reader[2], section);
            input_reader[0].Close(); input_reader[1].Close(); input_reader[2].Close();
            CInitialize_Information Initial = new CInitialize_Information(train, station, section);
        }
    }
}
