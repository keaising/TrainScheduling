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
using System.Windows.Threading;
using Microsoft.Win32;
using System.Collections.ObjectModel;

/// <summary>
/// 160724：需要解决动画重复播放时，对之前的图像的擦除处理，注意逻辑顺序
/// </summary>

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

        //计时器
        DispatcherTimer timer;
        int EventIndex = 0;
        int unitTimeSpan = 120;

        //用来记录列车位置刷新时候，删除grid.chirldren中的元素<trainID,chirldrenIndex>
        Dictionary<int, int> g_mapGrirdRailwayTrainIDandIndex = new Dictionary<int, int>();
        //用来擦除timetable的图像<trainID,chirldrenIndex>
        List<Dictionary<int, int>> g_mapGrirdTimetableTrainIDandIndexList = new List<Dictionary<int, int>>();

        List<CTrain> gtrain = new List<CTrain>();
        List<CRailwayStation> gstation = new List<CRailwayStation>();
        List<CRailwaySection> gsection = new List<CRailwaySection>();

        HasInitialized hasInitialized = new HasInitialized("已经成功初始化底图", "请先画出底图！");
        HasInitialized hasInputData = new HasInitialized("基础数据读取成功！", "未输入基础数据，请选择数据！");
        HasInitialized hasRunTSTA = new HasInitialized("不要调皮，你已经运行过TSTA算法咯！", "未运行任何调度算法，请先运行调度/调整算法！");
        HasInitialized hasDrawRailwayMap = new HasInitialized("路网图绘制成功！", "路网图为空，请先画出路网结构图！");
        HasInitialized hasDrawTimetable = new HasInitialized("时刻表绘制成功！", "请先绘制时刻表！");





        //button_click input data，这里输入data的方式后面需要改动
        private void ParameterSettingButton_Click(object sender, RoutedEventArgs e)
        {
            var chooseData = new ChooseDataWindow();
            chooseData.ShowDialog();

            var pathList = BaseDataModel.List;

            //if (!hasInputData.Done)
            //{
            //    InputData(gtrain, gstation, gsection);
            //    hasInputData.Done = true;
            //    MessageBox.Show(hasInputData.Msg);
            //}

            //创建一个打开文件式的对话框  
            OpenFileDialog ofd = new OpenFileDialog();
            //设置这个对话框的起始打开路径  
            //ofd.InitialDirectory = @"D:\";            
            ofd.InitialDirectory =  Directory.GetParent(Environment.CurrentDirectory).Parent.ToString();
            //设置打开的文件的类型，注意过滤器的语法  
            ofd.Filter = "txt文本|*.txt";
            //调用ShowDialog()方法显示该对话框，该方法的返回值代表用户是否点击了确定按钮  
            if (ofd.ShowDialog() == true)
            {
                //image1.Source = new BitmapImage(new Uri(ofd.FileName));
            }
            else
            {
                MessageBox.Show("没有选择图片");
            }
        }

        /// <summary>
        /// button_click 画底图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DrawBasicPanel_Click(object sender, RoutedEventArgs e)
        {
            if (!hasInitialized.Done)
            {
                DrawBasicTimetable(gstation, gsection);
                hasInitialized.Done = true;
            }
        }

        /// <summary>
        /// button_click 运行TSTA算法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AlgTSTA_Click(object sender, RoutedEventArgs e)
        {
            if (hasInputData.Done && !hasRunTSTA.Done)
            {
                CTATS railwaySys = new CTATS(gtrain, gstation, gsection, 0);
                MessageBox.Show("TSTA算法运行成功！");
                hasRunTSTA.Done = true;
            }
            else
            {
                if (!hasInputData.Done)
                    MessageBox.Show(hasInputData.Msg);
                if (hasRunTSTA.Done)
                    MessageBox.Show(hasRunTSTA.Msg);
            }
        }

        /// <summary>
        /// button_click 画运行图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DrawTimetable_Click(object sender, RoutedEventArgs e)
        {
            if (hasInitialized.Done && hasInputData.Done && hasDrawTimetable.Done && hasRunTSTA.Done)
            {
                //画出运行图
                DisplayTrainTimeTable(gtrain, unitTimeSpan, gsection);
                hasDrawTimetable.Done = true;
                //展示结果
                DisplayResults(gtrain);
                //铁路线路图像
                DrawRailwayMap(gstation, gsection);
                hasDrawRailwayMap.Done = true;
            }
            else
            {
                ShowHasNotInitErrorMsg(new List<HasInitialized> { hasDrawTimetable, hasInputData, hasInitialized, hasRunTSTA });
            }
        }


        //动画演示
        private void DevideButton_Click(object sender, RoutedEventArgs e)
        {
            //update e
            EventIndex = 0;

            if (hasInputData.Done && hasRunTSTA.Done && hasDrawRailwayMap.Done)
            {
                int mapIndexStart; int mapIndexEnd;
                if (g_mapGrirdTimetableTrainIDandIndexList.Count > 0)
                    if (g_mapGrirdTimetableTrainIDandIndexList[0].TryGetValue(0, out mapIndexStart)
                        && g_mapGrirdTimetableTrainIDandIndexList[g_mapGrirdTimetableTrainIDandIndexList.Count - 1].TryGetValue(gstation.Count - 2, out mapIndexEnd))
                        GridSchWinTimetable.Children.RemoveRange(mapIndexStart, mapIndexEnd);

                timer = new DispatcherTimer(DispatcherPriority.Normal);
                timer.Tick += new EventHandler(TimerTickMethod);
                timer.Interval = TimeSpan.FromMilliseconds(40);
                timer.Start();
            }
            else
            {
                ShowHasNotInitErrorMsg(new List<HasInitialized> { hasInputData, hasRunTSTA, hasDrawRailwayMap });
            }
        }

        /// <summary>
        /// 显示没有初始化的错误信息
        /// </summary>
        /// <param name="hasDones"></param>
        private void ShowHasNotInitErrorMsg(List<HasInitialized> hasDones)
        {
            foreach (var has in hasDones)
            {
                if (!has.Done)
                {
                    MessageBox.Show(has.Msg);
                }
            }
        }

        private void TimerTickMethod(object sender, EventArgs e)
        {
            //   throw new NotImplementedException();
            DynamicDisplayTrainTravel(gtrain, gsection, gstation, 1, 1);
        }

        private List<string> GetStationNames(List<CRailwayStation> inputstation)
        {
            List<CRailwayStation> station = new List<CRailwayStation>();
            //InputData(train, station, section);
            foreach (var obj in inputstation)
                station.Add(obj.Clone());
            List<string> StaName = new List<string>();
            for (int j = 0; j < station.Count; j++)
                StaName.Add("Station_" + station[station.Count - 1 - j].stationID.ToString());
            return StaName;
        }

        /// <summary>
        /// 画底图
        /// </summary>
        /// <param name="stations"></param>
        /// <param name="sections"></param>
        private void DrawBasicTimetable(List<CRailwayStation> stations, List<CRailwaySection> sections)
        {
            if (sections.Count == 0 || sections.Count == 0)
            {
                hasInitialized.Done = false;
                MessageBox.Show(hasInitialized.Msg);
                return;
            }

            //根据grid的划分，合理设计原点，保证时间坐标对齐，stationname 对齐
            //现在以读数据的方式将线路数据读进去，以后需要进一步考虑“点选方案”
            ClearAllChildren();

            //wsx: 这段为什么要这样赋值？
            List<CRailwayStation> stationes = new List<CRailwayStation>();
            List<CRailwaySection> sectiones = new List<CRailwaySection>();
            //InputData(train, station, section);
            foreach (var obj in stations)
                stationes.Add(obj.Clone());
            foreach (var obj in sections)
                sectiones.Add(obj.Clone());

            //input section length; station name et al.  
            List<double> sectionLengthes = new List<double>();
            List<double> timetableSectionLengthes = new List<double>();
            var stationNames = GetStationNames(stationes);

            for (int j = 0; j < sectiones.Count; j++)
            {
                sectionLengthes.Add(sectiones[sectiones.Count - 1 - j].length);
                timetableSectionLengthes.Add(sectiones[j].length);
            }

            //StaName.Add("北京南"); StaName.Add("蚌埠南"); StaName.Add("南京南"); StaName.Add("上海虹桥");           
            double routeLength = sectionLengthes.Sum(); //(in km)

            //// get H and W of grid          
            double panelHeight = GridSchWinTimetable.ActualHeight;
            double panelWidth = GridSchWinTimetable.ActualWidth;
            //get 分格信息 合理划分 W, 1440 分钟
            int sectionCount = stationNames.Count - 1;

            //wsx:这是什么？不要用简写
            double[] GTP = new double[8];
            GTP = GridTimeTableParameter(GridSchWinTimetable, unitTimeSpan, sectionLengthes);
            //wsx:自动赋值，不要挨个赋值，没有扩展性
            double originX = GTP[0];
            double originY = GTP[1];
            double timetableWidth = GTP[2];
            double timetableHight = GTP[3];
            double timeSpanInUnitMinute = GTP[4];
            double stationSpanInUnitKm = GTP[5];
            double timeInterval = GTP[6];
            double timespanCount = GTP[7];

            //draw time line
            for (int i = 0; i <= timespanCount * 4; i++)
            {
                var x1 = originX + i * timeInterval / 4; var x2 = originX + i * timeInterval / 4;
                var y1 = originY; var y2 = originY + panelHeight + 0.02 * panelHeight; //伸出的做为标识
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
            for (int i = 0; i <= sectionCount; i++)
            {
                var a1 = originX;
                var a2 = originX + timetableWidth;
                double b1 = 0;
                double b2 = 0;
                if (i == 0)
                {
                    b1 = originY;
                    b2 = originY;
                }
                else
                {
                    b1 = originY + sectionLengthes.SumFromTo(0, i - 1) * stationSpanInUnitKm;
                    b2 = originY + sectionLengthes.SumFromTo(0, i - 1) * stationSpanInUnitKm;
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


            //wsx：这个跟上面的stationNames有什么关系？
            List<string> strName = new List<string>();
            strName.Add("京");
            strName.Add("沪");
            strName.Add("线");
            //display railway line name
            for (int i = 0; i < strName.Count; i++)
            {
                RowDefinition rowdef = new RowDefinition();//创建行布局对向
                GridSchWinLineName.RowDefinitions.Add(rowdef);
                TextBlock TextBlockStationName = new TextBlock();
                //get station name
                TextBlockStationName.Text = strName[i];
                TextBlockStationName.FontSize = 14;
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
            for (int i = 0; i <= timespanCount + 1; i++)
            {
                ColumnDefinition coldef = new ColumnDefinition();//创建列布局对向
                GridSchWinTimeIndex.ColumnDefinitions.Add(coldef);
                TextBlock TextBlockaTimeSpanName = new TextBlock();
                //get timespan span
                var TimeNumber = 1440.0 / timespanCount;

                if (i == 0)
                {
                    TextBlockaTimeSpanName.Width = panelWidth / 6 - timeInterval / 2;
                    coldef.Width = new GridLength(panelWidth / 6 - timeInterval / 2);
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
                    //get font size
                    int fontsize = 10; //默认大概36多
                    if (timeInterval > 40 && timeInterval < 46) fontsize = 12;
                    if (timeInterval > 46 && timeInterval < 52) fontsize = 14;
                    if (timeInterval > 52) fontsize = 15;
                    TextBlockaTimeSpanName.FontSize = fontsize;
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

            //display railway station name
            for (int i = 0; i < stationNames.Count; i++)
            {
                RowDefinition rowdef = new RowDefinition();//创建行布局对向
                GridSchWinStationName.RowDefinitions.Add(rowdef);
                TextBlock TextBlockStationName = new TextBlock();
                //get station name
                TextBlockStationName.Text = stationNames[i];
                TextBlockStationName.Margin = new Thickness(0, 0, 5, 0);

                //根据车站数目和区间长度调整textblock的高度
                if (i == 0)
                    rowdef.Height = new GridLength(originY);
                else
                    rowdef.Height = new GridLength(sectionLengthes[i - 1] * stationSpanInUnitKm);
                //get font size
                int fontsize = 9; //y_origin 大概10多
                if (originY > 14 && originY <= 18) fontsize = 10;
                if (originY > 18 && originY <= 22) fontsize = 12;
                if (originY >= 22) fontsize = 14;
                TextBlockStationName.FontSize = fontsize;
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

        private void ClearAllChildren()
        {
            GridSchWinTimetable.Children.Clear();
            GridSchWinTimeIndex.Children.Clear();
            GridSchWinStationName.Children.Clear();
            GridSchWinLineName.Children.Clear();
            GridSchWinLineName.RowDefinitions.Clear();
            GridSchWinTimeIndex.ColumnDefinitions.Clear();
            GridSchWinStationName.RowDefinitions.Clear();
        }

        /// <summary>
        /// 动态演示列车的运行过程
        /// </summary>
        private void DynamicDisplayTrainTravel(List<CTrain> trains, List<CRailwaySection> sections, List<CRailwayStation> stations, int skipEventNum, int refreshTime)
        {
            int NumDiscreteEvent = trains[0].ListTime.Count;
            double H = GridSchWinRailwayMap.ActualHeight;
            double W = GridSchWinRailwayMap.ActualWidth;
            double originY = H / 2;
            double originX = W / 20;
            int NumSta = stations.Count;
            int NumSec = sections.Count;
            double TotalLength = 0;
            for (int i = 0; i < sections.Count; i++)
                TotalLength = TotalLength + sections[i].length;
            //线路长度比例,Wb为线路上每公里在图上的长度
            double WidthInUnitKm = 0.9 * W / TotalLength;

            if (EventIndex < NumDiscreteEvent)
            {
                DisplayTrainPosition(gtrain, EventIndex, originX, H, WidthInUnitKm);
                //wsx: 这里为什么还保罗RefreshTime这个参数
                //System.Threading.Thread.Sleep(RefreshTime * 1000); //1 second
                DynamicDisplayTrainTimetable(gtrain, unitTimeSpan, gsection, EventIndex);
                EventIndex += skipEventNum;
            }
            else
            {
                timer.Stop();
                MessageBox.Show("动画结束");
            }
        }

        /// <summary>
        /// 刷新列车位置
        /// </summary>       
        private void DisplayTrainPosition(List<CTrain> trains, int eventIndex, double originX, double H, double widthInUnitKm) //wsx: widthInUnitKm是什么
        {
            //擦除原来画面
            int mapIndexStart;
            int mapIndexEnd;
            //wsx: 三秒钟内告诉我这里判断了啥。。。
            if (g_mapGrirdRailwayTrainIDandIndex.TryGetValue(trains[0].trainID, out mapIndexStart) && g_mapGrirdRailwayTrainIDandIndex.TryGetValue(trains[trains.Count - 1].trainID, out mapIndexEnd))
                GridSchWinRailwayMap.Children.RemoveRange(mapIndexStart, mapIndexEnd);

            //清除map
            g_mapGrirdRailwayTrainIDandIndex.Clear();

            //画出新画面
            for (int i = 0; i < trains.Count; i++)
            {
                double position = trains[i].ListPosition[eventIndex] / 1000; //单位由米转换为千米  
                //出现列车   
                DisplayTrainImage(i, trains[i].trainID, trains[i].trainType, position, originX, H, widthInUnitKm);
            }
        }

        /// <summary>
        /// 出现列车图像
        /// </summary>    
        private void DisplayTrainImage(int i, int trainID, int TrainType, double position, double xorigin, double H, double WidthInUnitKm)
        {
            //g_mapTrainIDandIndex.Clear();
            CParameter parameter = new CParameter();
            double UnitH = H / 20;
            double y1 = H / 2 - 3 * UnitH, y2 = H / 2 - 2 * UnitH;
            RectangleGeometry myRectangleGeometry = new RectangleGeometry();
            var myPath = new System.Windows.Shapes.Path();
            myPath.Stroke = System.Windows.Media.Brushes.Black;
            myPath.StrokeThickness = 1;

            int ColorIndex = i % (parameter.HexCode.Count() - 1);
            if (TrainType == 0)
            {
                var color = new System.Windows.Media.Color();//               
                var SDcolor = System.Drawing.Color.FromName(parameter.HexCode[ColorIndex]);
                color.R = SDcolor.R;
                color.G = SDcolor.G;
                color.B = SDcolor.B;
                color.A = SDcolor.A;
                myPath.Fill = new SolidColorBrush(color);
                myRectangleGeometry.Rect = new Rect(xorigin + position * WidthInUnitKm, H / 2 - (1.4 * UnitH), 8 * WidthInUnitKm, 1.4 * UnitH);
            }
            else
            {
                var color = new System.Windows.Media.Color();//               
                var SDcolor = System.Drawing.Color.FromName(parameter.HexCode[ColorIndex]);
                color.R = SDcolor.R;
                color.G = SDcolor.G;
                color.B = SDcolor.B;
                color.A = SDcolor.B;
                myPath.Fill = new SolidColorBrush(color);
                //myPath.Fill = System.Windows.Media.Brushes.YellowGreen;
                myRectangleGeometry.Rect = new Rect(xorigin + position * WidthInUnitKm, H / 2, 8 * WidthInUnitKm, 1.4 * UnitH);
            }
            myPath.Data = myRectangleGeometry;
            GridSchWinRailwayMap.Children.Add(myPath);
            //给出trainID对应的Index在刷新的时候删除GridSchWinRailwayMap中对应的元素          
            g_mapGrirdRailwayTrainIDandIndex.Add(trainID, GridSchWinRailwayMap.Children.Count - 1);
        }

        /// <summary>
        /// 铁路线路基本图
        /// </summary>    
        private void DrawRailwayMap(List<CRailwayStation> station, List<CRailwaySection> sections)
        {
            GridSchWinRailwayMap.Children.Clear();
            GridSchWinRailwayMapStationName.Children.Clear();
            GridSchWinRailwayMapStationName.ColumnDefinitions.Clear();

            double H = GridSchWinRailwayMap.ActualHeight;
            double W = GridSchWinRailwayMap.ActualWidth;
            double originY = H / 2;
            double originX = W / 20;
            var lengthSum = sections.Sum(e => e.length);
            //线路长度比例,Wb为线路上每公里在图上的长度
            double WidthInUnitKm = 0.9 * W / lengthSum;

            List<double> sectionLengthes = new List<double>();
            for (int j = 0; j < sections.Count; j++)
                sectionLengthes.Add(sections[j].length);

            //比照铁路作图，画出25个白色格子，35个黑色格子，组成铁路线路
            RailwayBlackWhiteGrid(originX, originY, 0.9 * W, 35);
            //画终端站
            RailwayTerminal(originX, originY, H, 0.9 * W);
            //画中间车站布局
            for (int i = 1; i < station.Count - 1; i++)
                DrawRailwayStaion(sectionLengthes.SumFromTo(0, i) * WidthInUnitKm + originX, station[i].stationCapacity, originX, H);

            List<string> stationNames = new List<string>();
            stationNames = GetStationNames(station);
            //输出车站名称           
            PrintRailwayStationName(stationNames, sectionLengthes, originX, WidthInUnitKm);
        }

        /// <summary>
        /// 画铁路线路
        /// </summary>      
        private void RailwayBlackWhiteGrid(double originX, double originY, double L, int Num)
        {
            double unitW = L / (2 * Num);
            for (int i = 0; i < Num; i++)
            {
                //plot black grid
                var RailwayBlack = new Line();
                RailwayBlack.Stroke = System.Windows.Media.Brushes.Black;
                RailwayBlack.StrokeThickness = 3;
                //plot RailwayGray grid
                var RailwayGray = new Line();
                RailwayGray.Stroke = System.Windows.Media.Brushes.LightGray;
                RailwayGray.StrokeThickness = 3;
                RailwayBlack.X1 = originX + 2 * i * unitW; RailwayBlack.Y1 = originY;
                RailwayBlack.X2 = originX + (2 * i + 1) * unitW; RailwayBlack.Y2 = originY;

                RailwayGray.X1 = originX + (2 * i + 1) * unitW; RailwayGray.Y1 = originY;
                RailwayGray.X2 = originX + (2 * i + 2) * unitW; RailwayGray.Y2 = originY;
                GridSchWinRailwayMap.Children.Add(RailwayBlack);
                GridSchWinRailwayMap.Children.Add(RailwayGray);
            }
        }

        /// <summary>
        /// 画terminal station的布局
        /// </summary>      
        private void RailwayTerminal(double xorigin, double yorigin, double H, double W)
        {

            double unitH = H / 20;
            double y1 = H / 2 - 3 * unitH, y2 = H / 2 - 2 * unitH, y3 = H / 2 + 2 * unitH, y4 = H / 2 + 3 * unitH;
            double x1 = 0.66 * xorigin, x2 = xorigin, x3 = xorigin + W, x4 = x3 + 0.33 * xorigin;
            Line[] stationTracks = new Line[8];
            for (int i = 0; i < 8; i++)
            {
                stationTracks[i] = new Line();
                stationTracks[i].Stroke = System.Windows.Media.Brushes.Black;
                stationTracks[i].StrokeThickness = 3;
            }
            stationTracks[0].X1 = x1; stationTracks[0].Y1 = y1; stationTracks[0].X2 = x2; stationTracks[0].Y2 = y2;
            stationTracks[1].X1 = x1; stationTracks[1].Y1 = H / 2; stationTracks[1].X2 = x2; stationTracks[1].Y2 = H / 2;
            stationTracks[2].X1 = x1; stationTracks[2].Y1 = y4; stationTracks[2].X2 = x2; stationTracks[2].Y2 = y3;
            stationTracks[3].X1 = x2; stationTracks[3].Y1 = y2; stationTracks[3].X2 = x2; stationTracks[3].Y2 = y3;
            stationTracks[4].X1 = x3; stationTracks[4].Y1 = y2; stationTracks[4].X2 = x4; stationTracks[4].Y2 = y1;
            stationTracks[5].X1 = x3; stationTracks[5].Y1 = H / 2; stationTracks[5].X2 = x4; stationTracks[5].Y2 = H / 2;
            stationTracks[6].X1 = x3; stationTracks[6].Y1 = y3; stationTracks[6].X2 = x4; stationTracks[6].Y2 = y4;
            stationTracks[7].X1 = x3; stationTracks[7].Y1 = y2; stationTracks[7].X2 = x3; stationTracks[7].Y2 = y3;
            for (int i = 0; i < 8; i++)
                GridSchWinRailwayMap.Children.Add(stationTracks[i]);
        }

        /// <summary>
        /// 画station的布局
        /// </summary>      
        private void DrawRailwayStaion(double CenterX, double NumTrack, double xorigin, double H) //wsx: NumTrack没有使用
        {
            double UnitH = H / 22;
            double y1 = H / 2 - 2 * UnitH, y2 = H / 2, y3 = H / 2 + 2 * UnitH;
            double x1 = CenterX - 0.4 * xorigin, x2 = CenterX - 0.3 * xorigin, x3 = CenterX + 0.1 * xorigin, x4 = CenterX + 0.2 * xorigin;
            double x5 = CenterX - 0.2 * xorigin, x6 = CenterX - 0.1 * xorigin, x7 = CenterX + 0.3 * xorigin, x8 = CenterX + 0.4 * xorigin;
            Line[] StationTrack = new Line[6];
            for (int i = 0; i < 6; i++)
            {
                StationTrack[i] = new Line();
                StationTrack[i].Stroke = System.Windows.Media.Brushes.Black;
                StationTrack[i].StrokeThickness = 2;
            }
            StationTrack[0].X1 = x1; StationTrack[0].Y1 = y2; StationTrack[0].X2 = x2; StationTrack[0].Y2 = y1;
            StationTrack[1].X1 = x2; StationTrack[1].Y1 = y1; StationTrack[1].X2 = x3; StationTrack[1].Y2 = y1;
            StationTrack[2].X1 = x3; StationTrack[2].Y1 = y1; StationTrack[2].X2 = x4; StationTrack[2].Y2 = y2;
            StationTrack[3].X1 = x5; StationTrack[3].Y1 = y2; StationTrack[3].X2 = x6; StationTrack[3].Y2 = y3;
            StationTrack[4].X1 = x6; StationTrack[4].Y1 = y3; StationTrack[4].X2 = x7; StationTrack[4].Y2 = y3;
            StationTrack[5].X1 = x7; StationTrack[5].Y1 = y3; StationTrack[5].X2 = x8; StationTrack[5].Y2 = y2;
            for (int i = 0; i < 6; i++)
                GridSchWinRailwayMap.Children.Add(StationTrack[i]);
        }


        /// <summary>
        /// 输出station name of railway line 
        /// </summary>       
        private void PrintRailwayStationName(List<string> stationNames, List<double> sectionLengthes, double originX, double widthInUnitKm)
        {
            for (int i = 0; i < stationNames.Count; i++)
            {
                ColumnDefinition columndef = new ColumnDefinition();//创建列布局对向
                GridSchWinRailwayMapStationName.ColumnDefinitions.Add(columndef);
                TextBlock TextBlockStationName = new TextBlock();
                //get station name
                TextBlockStationName.Text = stationNames[stationNames.Count - 1 - i];
                //get font size
                int fontsize = 10;
                if (widthInUnitKm > 1 && widthInUnitKm < 1.2) fontsize = 14;
                if (widthInUnitKm > 1.2) fontsize = 15;
                TextBlockStationName.FontSize = fontsize;
                //根据车站数目和区间长度调整textblock的高度
                if (i == 0)
                {
                    //这里的1.4参考车站布局的画法，track长度横跨中心点的0.2origin左右
                    columndef.Width = new GridLength(1.4 * originX);
                    TextBlockStationName.Width = 1.4 * originX;
                }
                else
                {
                    columndef.Width = new GridLength(sectionLengthes[i - 1] * widthInUnitKm);
                    TextBlockStationName.Width = sectionLengthes[i - 1] * widthInUnitKm;
                }

                var color = new System.Windows.Media.Color(); color.R = 55; color.G = 0; color.B = 255; color.A = 255;
                TextBlockStationName.Foreground = new SolidColorBrush(color);
                TextBlockStationName.FontFamily = new System.Windows.Media.FontFamily("Times New Roman");
                TextBlockStationName.TextAlignment = TextAlignment.Right;
                //设置每个textBlock的Margin
                TextBlockStationName.Margin = new Thickness(0, 0, 0, 0);
                TextBlockStationName.VerticalAlignment = VerticalAlignment.Top;
                TextBlockStationName.HorizontalAlignment = HorizontalAlignment.Center;
                Grid.SetColumn(TextBlockStationName, i);
                TextBlockStationName.FontStretch = FontStretches.Medium;//100%，紧缩或加宽的程度
                GridSchWinRailwayMapStationName.Children.Add(TextBlockStationName);
            }
        }

        /// <summary>
        /// 拖动窗口大小之后重绘
        /// </summary>        
        private void MetroWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var t1 = DateTime.Now;
            if (hasInitialized.Done)
            {
                var testRectangle = new System.Windows.Shapes.Rectangle();
                testRectangle.StrokeThickness = 1.5;
                testRectangle.Stroke = System.Windows.Media.Brushes.Green;
                testRectangle.Width = 50; testRectangle.Height = 20;
                testRectangle.HorizontalAlignment = HorizontalAlignment.Left;
                testRectangle.VerticalAlignment = VerticalAlignment.Top;
                GridSchWinTimeIndex.Children.Add(testRectangle);

                DrawBasicTimetable(gstation, gsection);
            }

            if (hasDrawTimetable.Done)
                DisplayTrainTimeTable(gtrain, unitTimeSpan, gsection);

            if (hasDrawRailwayMap.Done)
                DrawRailwayMap(gstation, gsection);

        }

        /// <summary>
        /// 画运行图
        /// </summary>      
        private void DisplayTrainTimeTable(List<CTrain> train, int GivenFengeInUnitTimeSpan, List<CRailwaySection> Section)
        {
            List<double> SectionLength = new List<double>();
            for (int j = 0; j < Section.Count; j++)
                SectionLength.Add(Section[j].length);

            int TimeMaxIndex = 0;
            foreach (CTrain obj in train)
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
                var g_mapGrirdTimetableTrainIDandIndex = new Dictionary<int, int>();
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
                    g_mapGrirdTimetableTrainIDandIndex.Add(j, GridSchWinTimetable.Children.Count - 1);
                }
                g_mapGrirdTimetableTrainIDandIndexList.Add(g_mapGrirdTimetableTrainIDandIndex);
            }
        }

        //逐事件演示列车运行图
        private void DynamicDisplayTrainTimetable(List<CTrain> train, int GivenFengeInUnitTimeSpan, List<CRailwaySection> Section, int EnventIndex)
        {
            CParameter parameter = new CParameter();
            List<double> SectionLength = new List<double>();
            for (int j = 0; j < Section.Count; j++)
                SectionLength.Add(Section[j].length);

            int TimeMaxIndex = 0;
            foreach (CTrain obj in train)
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

            //清除map
            g_mapGrirdTimetableTrainIDandIndexList.Clear();
            //画出新的运行线          
            if (EventIndex > 0)
                for (int i = 0; i < train.Count(); i++)
                    if (train[i].ListPosition[EventIndex] >= 0 && train[i].ListPosition[EventIndex - 1] >= 0)
                    {
                        int ColorIndex = i % (parameter.HexCode.Count() - 1);

                        //plot travelling time line 
                        var TravellingTimeLine = new Line();

                        var color = new System.Windows.Media.Color();//               
                        var SDcolor = System.Drawing.Color.FromName(parameter.HexCode[ColorIndex]);
                        color.R = SDcolor.R;
                        color.G = SDcolor.G;
                        color.B = SDcolor.B;
                        color.A = SDcolor.B;
                        //myPath.Fill = new SolidColorBrush(color);
                        TravellingTimeLine.Stroke = new SolidColorBrush(color);

                        //TravellingTimeLine.Stroke = System.Windows.Media.Brushes.DarkBlue;
                        TravellingTimeLine.StrokeThickness = 1.5;
                        double tx1, ty1, tx2, ty2;
                        tx1 = TimeSpanInUnitMinute * train[i].ListTime[EventIndex - 1] / 60 + x_origin;
                        ty1 = H - StationSpanInUnitKm * (train[i].ListPosition[EventIndex - 1] / 1000);
                        tx2 = TimeSpanInUnitMinute * train[i].ListTime[EventIndex] / 60 + x_origin;
                        ty2 = H - StationSpanInUnitKm * (train[i].ListPosition[EventIndex] / 1000);
                        TravellingTimeLine.X1 = tx1;
                        TravellingTimeLine.X2 = tx2;
                        TravellingTimeLine.Y1 = ty1;
                        TravellingTimeLine.Y2 = ty2;
                        GridSchWinTimetable.Children.Add(TravellingTimeLine);
                    }
        }


        /// <summary>
        /// Display results generated by scheduling algorithm
        /// </summary>
        private void DisplayResults(List<CTrain> train)
        {
            string[] ResultStr = new string[6];
            List<int> MaxDelayTrainID = new List<int>();
            double TotalDelay = 0; double MaxDelay = 0;
            double ClearTimeStart = 0; double ClearTimeEnd = 0; double ClearTime = 0;
            for (int i = 0; i < train.Count; i++)
            {
                TotalDelay = TotalDelay + train[i].DelayTime;
                MaxDelay = Math.Max(MaxDelay, train[i].DelayTime);
                ClearTimeStart = Math.Min(ClearTimeStart, train[i].arrival[0]);
                ClearTimeEnd = Math.Max(ClearTimeEnd, train[i].departure[train[i].route[train[i].route.Count - 1]]);
            }
            ClearTime = ClearTimeEnd - ClearTimeStart;

            string MTID = "";
            for (int i = 0; i < train.Count; i++)
            {
                if (train[i].DelayTime == MaxDelay)
                {
                    MaxDelayTrainID.Add(train[i].trainID);
                    MTID = MTID + train[i].trainID + "；";
                }
            }

            ResultStr[0] = (int)(TotalDelay / 60) + " 分钟";
            ResultStr[1] = (int)(MaxDelay / 60) + " 分钟";
            ResultStr[2] = MTID;
            ResultStr[3] = "列车总能耗";

            ObservableCollection<CDisplayData> displaylist = new ObservableCollection<CDisplayData>();

            displaylist.Add(new CDisplayData()
            {
                Name = "当前算法",
                Outputdata = "TATS"
            });
            displaylist.Add(new CDisplayData()
            {
                Name = "总延迟",
                Outputdata = ResultStr[0]
            });
            displaylist.Add(new CDisplayData()
            {
                Name = "最大延迟",
                Outputdata = ResultStr[1]
            });
            displaylist.Add(new CDisplayData()
            {
                Name = "最大延迟列车",
                Outputdata = MTID
            });
            displaylist.Add(new CDisplayData()
            {
                Name = "线路清空时间",
                Outputdata = (int)(ClearTime / 60) + " 分钟"
            });
            displaylist.Add(new CDisplayData()
            {
                Name = "总能耗",
                Outputdata = "null"
            });
            //this.ResResultStatisticListData.ItemsSource = displaylist;
            this.ResResultStatisticListData.DataContext = displaylist;

            ObservableCollection<CDisplayTrainData> trainData = new ObservableCollection<CDisplayTrainData>();
            foreach (var obj in train)
            {
                trainData.Add(new CDisplayTrainData()
                {
                    ID = obj.trainID.ToString() as string,
                    speed = obj.speed.ToString() as string,
                    delayTime = obj.DelayTime.ToString() + "/" + (int)(obj.DelayTime / 60) as string,
                    energy = "null" as string
                });
            }
            this.ResResultTrainGridData.DataContext = trainData;
        }

        /// <summary>
        /// 返回grid参数
        /// </summary>       
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
        private void InputData(List<CTrain> train, List<CRailwayStation> station, List<CRailwaySection> section)
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

        private void DisplayOtherData_MouseEnter(object sender, MouseEventArgs e)
        {
            DisplayOtherDataTB.Background = CmyColor.RequiredColor("LightBlue");
        }
        private void DisplayOtherDataTB_MouseLeave(object sender, MouseEventArgs e)
        {
            DisplayOtherDataTB.Background = CmyColor.RequiredColor("White");
        }
        private void DispalyScheDataTB_MouseEnter(object sender, MouseEventArgs e)
        {
            DispalyScheDataTB.Background = CmyColor.RequiredColor("LightBlue");
        }
        private void DispalyScheDataTB_MouseLeave(object sender, MouseEventArgs e)
        {
            DispalyScheDataTB.Background = CmyColor.RequiredColor("White");
        }
    }


}
