using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using TrainScheduling.Model;

//本程序用于Reschedule trains in subway system; 大体上分为三个部分 故障前、故障中和故障后
//采用的是一种基于离散事件的贪婪的调度算法

namespace TrainScheduling.Algorithm
{
    public class CRescheduling
    {
        public CRescheduling()
        {
            string defaultPath = System.IO.Directory.GetCurrentDirectory().ToString();//读取txt文件程序
            FileInfo data0 = new FileInfo(defaultPath + "\\data.txt");
            //FileInfo outdata = new FileInfo(defaultPath + "\\outdata.txt");
            
            FileStream Version_data = data0.Open(FileMode.Open);
            StreamReader data = new StreamReader(Version_data, System.Text.Encoding.Default);

            FileStream foutdata = new FileStream(defaultPath + "\\outdata.txt", FileMode.Append, FileAccess.Write);
            StreamWriter outdata = new StreamWriter(foutdata);
            
            
            string inputdata; int j = 0; string w1, w2, w3;
            int t0 = 0, tr = 0, Headway = 0; //表示故障发生时间，结束时间，以及发车间隔时间
            int[] OutArrival = new int[14];//outbound arrival time
            int[] OutDeparture = new int[14];//outbound departure time
            int[] InArrival = new int[14];//Inbound arrival time
            int[] InDeparture = new int[14];//inbound departure time

            while ((inputdata = data.ReadLine()) != null) //读取data.txt文件程序
            {
                string[] arrstr = inputdata.Split(',');//using 'space' to split words;
                w1 = arrstr[0].ToString(); w2 = arrstr[1].ToString(); w3 = arrstr[2].ToString();

                if (j == 1)//读取t0，tr，headway
                {
                    t0 = int.Parse(w1); tr = int.Parse(w2); Headway = int.Parse(w3); 
                }

                else if (j >= 3 && j <= 16)//读取outbound
                {
                    OutArrival[j - 3] = int.Parse(w2); OutDeparture[j - 3] = int.Parse(w3);
                }

                else if (j >= 17 && j <= 30)//读取inbound 
                {
                    InArrival[30 - j] = int.Parse(w2); InDeparture[30 - j] = int.Parse(w3);
                }

                j++; //行号加一
            }

            int N = 40;//计划规划36列车
            int K = 14;//number of station
            int K1 = 2, K2 = 3; //故障区间

            CTrain[] TrainOr = new CTrain[N]; //原始schedule；
            CTrain[] TrainNew = new CTrain[N]; //新的schedule;
            for (int i = 0; i < N; i++) 
            {
                TrainOr[i] = new CTrain();
                TrainNew[i] = new CTrain();
                TrainOr[i].arrival = new int[K];
                TrainOr[i].departure = new int[K];
                TrainNew[i].arrival = new int[K];
                TrainNew[i].departure = new int[K];
                TrainOr[i].sectiontime = new int[K - 1];
                TrainNew[i].sectiontime = new int[K - 1];
                 TrainOr[i].SubDwellTime = new int[K];
                TrainNew[i].SubDwellTime = new int[K];
            }

            for (int i = 0; i < N; i++) //original timetable
            {
                int h = (int)(i / 2);
                //Outbound trains
                if (i % 2 == 0)
                {
                    for (int k = 0; k < K; k++)
                    {
                        TrainOr[i].arrival[k] = OutArrival[k] + h * Headway;
                        TrainOr[i].departure[k] = OutDeparture[k] + h * Headway;
                    }
                    TrainNew[i].NextNode = 0;//列车均未出发，下一个目标站点的标号
                    TrainOr[i].trainType = 0;
                    TrainNew[i].trainType = 0;
                    TrainNew[i].PathType = 0;
                    TrainOr[i].PathType = 0;
                }
                //Inbound trains
                else if (i % 2 == 1)
                {
                    for (int k = 0; k < K; k++)
                    {
                        TrainOr[i].arrival[k] = InArrival[k] + h * Headway;
                        TrainOr[i].departure[k] = InDeparture[k] + h * Headway;
                    }
                    TrainNew[i].NextNode = K - 1;//列车均未出发，下一个目标站点的标号
                    TrainOr[i].trainType = 1;
                    TrainNew[i].trainType = 1;
                    TrainNew[i].PathType = 1;
                    TrainOr[i].PathType = 1;
                }
                TrainNew[i].bdeparture = 0; //列车均为出发
                TrainNew[i].Nodestatus = 0;
                TrainNew[i].LockIndex = 0;
            }

            //section trip time and dwell time
            for (int i = 0; i < N; i++) 
            {
                if (i % 2 == 0) 
                {
                    for (int k = 0; k < K; k++)
                    {
                        if (k < K - 1)
                        {
                            TrainOr[i].sectiontime[k] = TrainOr[i].arrival[k + 1] - TrainOr[i].departure[k];                                
                        }
                        TrainOr[i].SubDwellTime[k] = TrainOr[i].departure[k] - TrainOr[i].arrival[k];
                    }
                }

                else if (i % 2 == 1) 
                {
                    for (int k = 0; k < K; k++)
                    {
                        if (k > 0)
                        {
                            TrainOr[i].sectiontime[k - 1] = TrainOr[i].arrival[k - 1] - TrainOr[i].departure[k];
                        }
                        TrainOr[i].SubDwellTime[k] = TrainOr[i].departure[k] - TrainOr[i].arrival[k];
                    }
                }                    
            }

            int tc = 10; //列车经过crossover的时间？？
                        
            Stopwatch sw = new Stopwatch();
            sw.Start();
                    

            int TT = 18000; int runtime = 0; int M = 10000000;//M为一个极大数
            int DynamicCount = 0;
            while (runtime < TT)
            {
                runtime = runtime + 0;                       //runtime>2590&&runtime <=2597
                int Dy_Time = 0; int[] Dy_time = new int[N]; //最小离散时间，与离散数组
                int decide = 0; //决策计数
                int[] decideNum = new int[N];
                for (int i = 0; i < N; i++)
                { Dy_time[i] = -1; TrainNew[i].check = 0; decideNum[i] = -1; }

                //能力检测算法   1-通行；0-不通行;2-即将发车，为最高优先权
                int decide_num = 0; 
                {
                    //故障前:所有列车都按照其之前的timetable排定，是可行的
                    if (runtime < t0)
                    {
                        for (int i = 0; i < N; i++)
                        {
                            if (TrainNew[i].bdeparture == 0)
                            { TrainNew[i].check = 2; decide_num++; decideNum[i] = 1;}
                            else if (TrainNew[i].bdeparture == 1) { TrainNew[i].check = 1; decide_num++; decideNum[i] = 1; }

                            else if (TrainNew[i].bdeparture == 2) { TrainNew[i].check = 0; decide_num++; decideNum[i] = 1; }
                        }
                    }

                    //故障中
                        
                    else if (runtime >= t0 && runtime < tr)
                    {
                        for (int li = 0; li < N; li++) 
                        {
                            if (TrainNew[li].departure[K2 + 1] < t0 && TrainNew[li].departure[K2 + 1] != 0 && TrainNew[li].trainType == 1)//inbound 列车借用outbound区间
                            { TrainNew[li].PathType = 1; TrainNew[li].LockIndex = 1; }
                            else { TrainNew[li].PathType = 0; }
                        }

                        for (int i = 0; i < N; i++)
                        {
                            if (TrainNew[i].bdeparture == 0) //尚未出发的列车
                            {
                                TrainNew[i].check = 2; decide_num++; decideNum[i] = 1;
                            }

                            else if (TrainNew[i].bdeparture == 1) //已出发的列车
                            {
                                //在节点上的
                               // if (TrainNew[i].Nodestatus == 1 || TrainNew[i].Nodestatus == 2)
                                if (TrainNew[i].Nodestatus == 2)
                                {
                                    if (TrainNew[i].trainType == 0)
                                    {
                                        //在故障区间右边的
                                        if (TrainNew[i].NextNode > K1 || TrainNew[i].NextNode == -K)
                                        {
                                            TrainNew[i].check = 1; decide_num++; decideNum[i] = 1;
                                        }
                                        //在故障区间内的
                                        else if (TrainNew[i].NextNode == K1) //要避免冲突，这是关键点
                                        {
                                            int DXcount = 0;//对向列车计数
                                            int DXindex = -1;
                                            for (int dxj = 0; dxj < N; dxj++)
                                            {
                                                if (TrainNew[dxj].trainType == 1) //对向列车计数
                                                {
                                                    if (TrainNew[dxj].Nodestatus == 3) //列车dxj在区间
                                                    {
                                                        if (TrainNew[dxj].NextNode <= K2 && TrainNew[dxj].NextNode >= K1 && TrainNew[dxj].PathType == 0)
                                                        { DXcount++; }
                                                    }
                                                    else if (TrainNew[dxj].Nodestatus == 1 || TrainNew[dxj].Nodestatus == 2)//列车dxj在车站 :需要细致的考虑 站对站的情况 
                                                    {
                                                        if (TrainNew[dxj].NextNode < K2 && TrainNew[dxj].NextNode >= K1 - 1 && TrainNew[dxj].PathType == 0) //有对向列车在故障站点内
                                                        { DXcount++; }
                                                        else if (TrainNew[dxj].NextNode == K2) //处于两端对决状态
                                                        {
                                                            DXindex = dxj;
                                                            //if (i > dxj) { DXcount++; } //表示上一个对向列车已经让出一个车位了，此对向列车需要获得优先权
                                                        }
                                                    }
                                                }
                                            }
                                            int TXcount = 0;
                                            for (int tx = 0; tx < N; tx++) //同向列车计数
                                            {
                                                if (TrainNew[tx].trainType == 0)
                                                {
                                                    if (TrainNew[tx].Nodestatus == 3)
                                                    {
                                                        if (TrainNew[tx].NextNode >= K1 && TrainNew[tx].NextNode <= K2 + 1)
                                                        { TXcount++; }
                                                    }
                                                    else if (TrainNew[tx].Nodestatus == 1 || TrainNew[tx].Nodestatus == 2)
                                                    {
                                                        if (TrainNew[tx].NextNode > K1 && TrainNew[tx].NextNode <= K2 + 1)
                                                        {
                                                            TXcount++;
                                                        }
                                                    }
                                                }
                                            }

                                            if (DXcount > 0)
                                            {
                                                TrainNew[i].check = 0; decide_num++; decideNum[i] = 1;
                                            }
                                            //else { TrainNew[i].check = 1; decide_num++; decideNum[i] = 1; }
                                            else
                                            {
                                                if (DXindex >= 0)
                                                {
                                                    if (TrainNew[i].arrival[K1 - 1] <= TrainNew[DXindex].arrival[K2 + 1])
                                                    { TrainNew[i].check = 1; decide_num++; decideNum[i] = 1; }
                                                    else if (TrainNew[i].arrival[K1 - 1] > TrainNew[DXindex].arrival[K2 + 1])
                                                    { TrainNew[i].check = 0; decide_num++; decideNum[i] = 1; }
                                                }
                                                else { TrainNew[i].check = 1; decide_num++; decideNum[i] = 1; }
                                            }
                                        }

                                        //故障区间左边的    
                                        else if (TrainNew[i].NextNode < K1)
                                        {
                                            if (i > 0)
                                            {
                                                int Ofirst = N + 1; int Ifirst = N + 1; int No = 1; int NI = 0; int So = 0; int OptOI = -1;//OptOI is opt(Ofirst)>opt(Ifirst): =0, false; =1, true.
                                                for (int xj = 0; xj < N; xj++)
                                                {
                                                    if (xj != i)
                                                    {
                                                        if (TrainNew[xj].trainType == 0)
                                                        {
                                                            if (TrainNew[xj].NextNode <= K1 && ((TrainNew[xj].NextNode == TrainNew[i].NextNode && TrainNew[xj].Nodestatus == 3) || (TrainNew[xj].NextNode > TrainNew[i].NextNode && (TrainNew[xj].Nodestatus == 1 || TrainNew[xj].Nodestatus == 2))))
                                                            {
                                                                No++;
                                                                if (xj < Ofirst) { Ofirst = xj; }
                                                            }
                                                        }
                                                        if (TrainNew[xj].trainType == 1 && TrainNew[xj].NextNode >= K1 && TrainNew[xj].PathType == 0)
                                                        {
                                                            NI++;
                                                            if (xj < Ifirst) { Ifirst = xj; }
                                                        }
                                                    }
                                                }

                                                So = K1 - TrainNew[i].NextNode;

                                                if (No > 1 && NI > 0)
                                                {
                                                    if ((TrainNew[Ifirst].NextNode - K2 < K1 - TrainNew[Ofirst].NextNode) || (TrainNew[Ifirst].NextNode - K2 == K1 - TrainNew[Ofirst].NextNode && TrainNew[Ifirst].arrival[K2 + 1] <= TrainNew[Ofirst].arrival[K1 - 1]) || (TrainNew[Ifirst].NextNode < K2) || (TrainNew[Ifirst].NextNode == K2 && TrainNew[Ifirst].Nodestatus == 3) || TrainNew[i - 2].check == 0)
                                                    { OptOI = 0; }
                                                    else { OptOI = 1; }

                                                    if ((OptOI == 1 && No - 1 <= So) || (OptOI == 0 && No <= So))
                                                    { TrainNew[i].check = 1; decide_num++; decideNum[i] = 1; }
                                                    else { TrainNew[i].check = 0; decide_num++; decideNum[i] = 1; }
                                                }

                                                else if (No > 1)
                                                {
                                                    if (No - 1 <= So && TrainNew[i - 2].check == 1)
                                                    { TrainNew[i].check = 1; decide_num++; decideNum[i] = 1; }
                                                    else { TrainNew[i].check = 0; decide_num++; decideNum[i] = 1; }
                                                }
                                                else { TrainNew[i].check = 1; decide_num++; decideNum[i] = 1; }


                                                if (TrainNew[i].NextNode == 1 && TrainNew[i - 2].NextNode == TrainNew[i].NextNode && TrainNew[i - 2].Nodestatus < 3) //起点的终极改变，decide_num不自加
                                                { TrainNew[i].check = 0; }

                                            }
                                            else if (i == 0)
                                            {
                                                TrainNew[i].check = 1; decide_num++; decideNum[i] = 1;
                                            }
                                        }
                                    }

                                    else if (TrainNew[i].trainType == 1)
                                    {
                                        //故障站K2左边的
                                        if (TrainNew[i].NextNode < K2 || TrainNew[i].NextNode == -K)
                                        {
                                            if (TrainNew[i].NextNode >= K1 - 1) //列车在故障区间内的站中，是被禁行的
                                            {
                                                if (TrainNew[i].PathType == 0) //i借用了outbound的线路
                                                { TrainNew[i].check = 1; decide_num++; decideNum[i] = 1; }
                                                else if (TrainNew[i].PathType == 1) //i没有借用了outbound的线路
                                                { TrainNew[i].check = 0; decide_num++; decideNum[i] = 1; }       //lockedindex                                              
                                            }
                                            else if (TrainNew[i].NextNode < K1 - 1 || TrainNew[i].NextNode == -K)
                                            { TrainNew[i].check = 1; decide_num++; decideNum[i] = 1; }

                                        }

                                        //故障区间中的,要避免冲突，这是关键点
                                        else if (TrainNew[i].NextNode == K2)
                                        {
                                            int dxcount = 0; int dxindex = -1;
                                            for (int dxi = 0; dxi < N; dxi++)
                                            {
                                                if (TrainNew[dxi].trainType == 0)  //对向列车
                                                {
                                                    if (TrainNew[dxi].Nodestatus == 1 || TrainNew[dxi].Nodestatus == 2) //对向列车在节点
                                                    {
                                                        if (TrainNew[dxi].NextNode <= K2 + 1 && TrainNew[dxi].NextNode > K1) //在故障区间的节点上
                                                        { dxcount++; }
                                                        else if (TrainNew[dxi].NextNode == K1) //对决站点
                                                        {
                                                            dxindex = dxi;
                                                            //if (i > dxi) //表示上一个对向列车已经让出一个车位了，此对向列车需要获得优先权
                                                            //{ dxcount++; }
                                                        }
                                                    }
                                                    else if (TrainNew[dxi].Nodestatus == 3) //对向列车在区间
                                                    {
                                                        if (TrainNew[dxi].NextNode <= K2 + 1 && TrainNew[dxi].NextNode >= K1)
                                                        { dxcount++; }
                                                    }
                                                }
                                            }

                                            int TXcount = 0;
                                            for (int tx = 0; tx < N; tx++) //同向列车计数
                                            {
                                                if (TrainNew[tx].trainType == 1)
                                                {
                                                    if (TrainNew[tx].Nodestatus == 3)
                                                    {
                                                        if (TrainNew[tx].NextNode >= K1 && TrainNew[tx].NextNode <= K2)
                                                        { TXcount++; }
                                                    }
                                                    else if (TrainNew[tx].Nodestatus == 1 || TrainNew[tx].Nodestatus == 2)
                                                    {
                                                        if (TrainNew[tx].NextNode >= K1 - 1 && TrainNew[tx].NextNode < K2)
                                                        {
                                                            TXcount++;
                                                        }
                                                    }
                                                }
                                            }

                                            if (dxcount > 0)
                                            {
                                                TrainNew[i].check = 0; decide_num++; decideNum[i] = 1;
                                            }
                                            else
                                            {
                                                if (dxindex >= 0)
                                                {
                                                    if (TrainNew[i].arrival[K2 + 1] < TrainNew[dxindex].arrival[K1 - 1])
                                                    { TrainNew[i].check = 1; decide_num++; decideNum[i] = 1; }
                                                    //{ TrainNew[i].check = 1; TrainNew[i].PathType = 0; decide_num++; decideNum[i] = 1; }
                                                    else if (TrainNew[i].arrival[K2 + 1] >= TrainNew[dxindex].arrival[K1 - 1])
                                                    { TrainNew[i].check = 0; decide_num++; decideNum[i] = 1; }
                                                }
                                                else { TrainNew[i].check = 1; decide_num++; decideNum[i] = 1; }
                                                //{ TrainNew[i].check = 1; TrainNew[i].PathType = 0; decide_num++; decideNum[i] = 1; }
                                            }
                                        }

                                        //故障区间右边的
                                        else if (TrainNew[i].NextNode > K2)
                                        {
                                            if (i > 1)
                                            {
                                                int Ofirst = N + 1; int Ifirst = N + 1; int NI = 1; int NO = 0; int SI = 0; int OptIO = -1;//OptOI is opt(Ofirst)>opt(Ifirst): =0, false; =1, true.
                                                for (int xj = 0; xj < N; xj++)
                                                {
                                                    if (xj != i)
                                                    {
                                                        if (TrainNew[xj].trainType == 1)
                                                        {
                                                            //if (TrainNew[xj].NextNode >= K2 && ((TrainNew[xj].NextNode == TrainNew[i].NextNode && TrainNew[xj].Nodestatus == 3) || (TrainNew[xj].NextNode < TrainNew[i].NextNode && (TrainNew[xj].Nodestatus == 1 || TrainNew[xj].Nodestatus == 2))))
                                                            if (TrainNew[xj].NextNode >= K2 && ((TrainNew[xj].NextNode == TrainNew[i].NextNode && TrainNew[xj].Nodestatus == 3) || (TrainNew[xj].NextNode < TrainNew[i].NextNode)))
                                                            {
                                                                NI++;
                                                                if (xj < Ifirst) { Ifirst = xj; }
                                                            }
                                                        }
                                                        if (TrainNew[xj].trainType == 0 && TrainNew[xj].NextNode <= K2 + 1 && TrainNew[xj].NextNode >= 0) //>=0 train xj on the system
                                                        {
                                                            if (xj < Ofirst) { Ofirst = xj; }
                                                            NO++;
                                                        }
                                                    }
                                                }

                                                SI = TrainNew[i].NextNode - K2;

                                                if (NO > 0 && NI > 1)
                                                {
                                                    //     Ifirst latter than Ofirst                                              //Ifirst later depature
                                                    if ((TrainNew[Ifirst].NextNode - K2 > K1 - TrainNew[Ofirst].NextNode || ((TrainNew[Ifirst].NextNode - K2 == K1 - TrainNew[Ofirst].NextNode) && TrainNew[Ifirst].Nodestatus <= TrainNew[Ofirst].Nodestatus)) || (TrainNew[Ofirst].NextNode > K1 || (TrainNew[Ofirst].NextNode == K1 && TrainNew[Ofirst].Nodestatus == 3)) || TrainNew[i - 2].check == 0)
                                                    { OptIO = 0; }
                                                    else { OptIO = 1; }

                                                    if ((OptIO == 1 && NI - 1 <= SI) || (OptIO == 0 && NI <= SI))
                                                    { TrainNew[i].check = 1; decide_num++; decideNum[i] = 1; }
                                                    else { TrainNew[i].check = 0; decide_num++; decideNum[i] = 1; }
                                                }

                                                else if (NI > 1)
                                                {
                                                    if (NI - 1 <= SI && TrainNew[i - 2].check == 1)
                                                    { TrainNew[i].check = 1; decide_num++; decideNum[i] = 1; }
                                                    else { TrainNew[i].check = 0; decide_num++; decideNum[i] = 1; }
                                                }
                                                else { TrainNew[i].check = 1; decide_num++; decideNum[i] = 1; }


                                                if (TrainNew[i].NextNode == K - 2 && TrainNew[i - 2].NextNode == TrainNew[i].NextNode && TrainNew[i - 2].Nodestatus < 3) //起点的终极改变，decide_num不自加
                                                { TrainNew[i].check = 0; }

                                            }
                                            else if (i == 1)
                                            {
                                                TrainNew[i].check = 1; decide_num++; decideNum[i] = 1;
                                            }
                                        }
                                    }
                                }


                                //不在节点上的,在区间的一律放行
                                if (TrainNew[i].Nodestatus == 1 || TrainNew[i].Nodestatus == 3)
                                { TrainNew[i].check = 1; decide_num++; decideNum[i] = 1; }
                            }

                            else if (TrainNew[i].bdeparture == 2) //已到达终点的列车
                            {
                                TrainNew[i].check = 0; decide_num++; decideNum[i] = 1;
                            }
                        }
                    }

                    //故障后
                    else if (runtime >= tr) //主要考虑的是inbound列车中 驶出故障区间的问题
                    {
                        for (int li = 0; li < N; li++)
                        {
                            if (TrainNew[li].departure[K2 + 1] >= t0 && TrainNew[li].departure[K2 + 1] < tr && TrainNew[li].departure[K2 + 1] != 0 && TrainNew[li].trainType == 1) //从整体的角度来说，在故障结束之前进入了outbound方向
                            { TrainNew[li].PathType = 0; }
                            else { TrainNew[li].PathType = 1; }

                            if (TrainNew[li].arrival[K2] > 0 && TrainNew[li].trainType == 1)
                            { TrainNew[li].PathType = 1; }
                        }

                        for (int i = 0; i < N; i++)
                        {
                            if (TrainNew[i].bdeparture == 0)
                            { TrainNew[i].check = 2; decide_num++; decideNum[i] = 1;}
                            else if (TrainNew[i].bdeparture == 1) 
                            {
                                //if (TrainNew[i].Nodestatus == 1 || TrainNew[i].Nodestatus == 2)
                                if (TrainNew[i].Nodestatus == 2)
                                {
                                    if (TrainNew[i].trainType == 0) //线路畅通状态下，所有列车均按照最初的状态运行，也许会有问题
                                    {
                                        //在故障区间右边的
                                        if (TrainNew[i].NextNode > K1 || TrainNew[i].NextNode == -K)
                                        {
                                            TrainNew[i].check = 1; decide_num++; decideNum[i] = 1;
                                        }

                                        else if (TrainNew[i].NextNode == K1)
                                        {
                                            int dxcount = 0;
                                            for (int dxi = 0; dxi < N; dxi++)
                                            {
                                                if (TrainNew[i].trainType == 1)//&&TrainNew[i].NextNode>=K1&&TrainNew[i].NextNode<K2
                                                {
                                                    if (TrainNew[dxi].Nodestatus == 3) //列车dxi在区间
                                                    {
                                                        if (TrainNew[dxi].NextNode <= K2 && TrainNew[dxi].NextNode >= K1 && TrainNew[dxi].PathType == 0)
                                                        { dxcount++; }
                                                    }
                                                    else if (TrainNew[dxi].Nodestatus == 1 || TrainNew[dxi].Nodestatus == 2)//列车dxj在车站 :需要细致的考虑 站对站的情况 
                                                    {
                                                        if (TrainNew[dxi].NextNode < K2 && TrainNew[dxi].NextNode >= K1 - 1 && TrainNew[dxi].PathType == 0) //有对向列车在故障站点内
                                                        { dxcount++; }
                                                        //else if (TrainNew[dxi].NextNode == K2) //此处dxi可使用inbound的路线                                                    
                                                    }
                                                }
                                            }
                                            if (dxcount > 0)
                                            {
                                                TrainNew[i].check = 0; decide_num++; decideNum[i] = 1;
                                            }
                                            else { TrainNew[i].check = 1; decide_num++; decideNum[i] = 1; }
                                        }

                                        else if (TrainNew[i].NextNode < K1)//K1左边
                                        {
                                            int dxcount = 0;
                                            for (int li = 0; li < N; li++)
                                            {
                                                if (TrainNew[li].trainType == 1 && TrainNew[li].PathType == 0)
                                                { dxcount++; }
                                            }

                                            if (dxcount > 0) //如果存在借用outbound的对向列车
                                            {
                                                int No = 1; int So = 0;
                                                for (int xj = 0; xj < N; xj++)
                                                {
                                                    if (xj != i)
                                                    {
                                                        if (TrainNew[xj].trainType == 0)
                                                        {
                                                            if (TrainNew[xj].NextNode <= K1 && ((TrainNew[xj].NextNode == TrainNew[i].NextNode && TrainNew[xj].Nodestatus == 3) || (TrainNew[xj].NextNode > TrainNew[i].NextNode && (TrainNew[xj].Nodestatus == 1 || TrainNew[xj].Nodestatus == 2))))
                                                            { No++; }
                                                        }
                                                    }
                                                }

                                                So = K1 - TrainNew[i].NextNode;
                                               
                                                //if (No <= So)
                                                if (No <= So || (TrainNew[i - 2].check == 1 && No - 1 <= So && TrainNew[i - 2].NextNode == K1))
                                                { TrainNew[i].check = 1; decide_num++; decideNum[i] = 1; }
                                                else { TrainNew[i].check = 0; decide_num++; decideNum[i] = 1; }

                                                if (i > 0)
                                                {
                                                    if (TrainNew[i].NextNode == 1 && TrainNew[i - 2].NextNode == TrainNew[i].NextNode && TrainNew[i - 2].Nodestatus < 3) //起点的终极改变，decide_num不自加
                                                    { TrainNew[i].check = 0; }

                                                    if (TrainNew[i].NextNode == 1 && runtime - TrainNew[i - 2].departure[0] < Headway)//考虑到发车的headway 间隔
                                                    { TrainNew[i].check = 0; }
                                                }
                                                
                                            }

                                            else //如果不存在借用outbound的对向列车
                                            {
                                                TrainNew[i].check = 1; decide_num++; decideNum[i] = 1;

                                                if (i > 0)
                                                {
                                                    if (TrainNew[i].NextNode == 1 && TrainNew[i - 2].NextNode == TrainNew[i].NextNode && TrainNew[i - 2].Nodestatus < 3) //起点的终极改变，decide_num不自加
                                                    { TrainNew[i].check = 0; }

                                                    if (TrainNew[i].NextNode == 1 && runtime - TrainNew[i - 2].departure[0] < Headway)//考虑到发车的headway 间隔
                                                    { TrainNew[i].check = 0; }
                                                }
                                            }
                                        }
                                    }

                                    else if (TrainNew[i].trainType == 1) //重点：考虑inbound列车中 驶出故障区间的问题
                                    {
                                        int txcount = 0; int txcheck = 0; int ljcount = 0;

                                        if (TrainNew[i].LockIndex == 1)
                                        {
                                            if (TrainNew[i].NextNode >= K1 - 1 && TrainNew[i].NextNode < K2)
                                            {
                                                for (int txj = 0; txj < N; txj++)
                                                {
                                                    if (TrainNew[txj].NextNode == TrainNew[i].NextNode && TrainNew[txj].trainType == 1 && txj != i) //在
                                                    {
                                                        if (TrainNew[txj].Nodestatus == 3)
                                                        { txcount++; } //符合条件计数
                                                        else { txcheck++; }
                                                    }
                                                }

                                                if (txcount <= 0) { TrainNew[i].check = 1; decide_num++; decideNum[i] = 1; }
                                                else if (txcount > 0) { TrainNew[i].check = 0; decide_num++; decideNum[i] = 1; }
                                            }
                                            else 
                                            { TrainNew[i].check = 1; decide_num++; decideNum[i] = 1; }
                                        }

                                        else //TrainNew[i].LockIndex == 0
                                        {  
                                            for (int lj = 0; lj < N; lj++)
                                            {
                                                if (TrainNew[lj].PathType == 0 && TrainNew[lj].trainType == 1)
                                                {
                                                    ljcount++;
                                                }
                                            }

                                            if (ljcount >= 0) //如果存在借用outbound的对向列车
                                            {
                                                if (TrainNew[i].NextNode == K1 - 1) //注意与locked train 的优先问题
                                                {
                                                    int count = 0;
                                                    for (int lj = 0; lj < N; lj++)
                                                    {
                                                        if (TrainNew[i].NextNode == TrainNew[lj].NextNode && lj < i && TrainNew[lj].LockIndex == 1) //存在locked train 下一步要使用同一个nextnode
                                                        { count++; }                                                        
                                                    }
                                                    if (count > 0) { TrainNew[i].check = 0; decide_num++; decideNum[i] = 1; }
                                                    else { TrainNew[i].check = 1; decide_num++; decideNum[i] = 1; }
                                                }

                                                else if (TrainNew[i].NextNode < K1 - 1) //
                                                { TrainNew[i].check = 1; decide_num++; decideNum[i] = 1; }

                                                else if (TrainNew[i].NextNode > K1 - 1) //同向能力问题
                                                {
                                                    int NI = 1; int SI = 0;
                                                    for (int xj = 0; xj < N; xj++)
                                                    {
                                                        if (xj != i)
                                                        {
                                                            if (TrainNew[xj].trainType == 1)
                                                            {
                                                                //(TrainNew[xj].NextNode >= K2 && ((TrainNew[xj].NextNode == TrainNew[i].NextNode && TrainNew[xj].Nodestatus == 3) || (TrainNew[xj].NextNode < TrainNew[i].NextNode)))
                                                                if (TrainNew[xj].NextNode >= K1 && ((TrainNew[xj].NextNode == TrainNew[i].NextNode && TrainNew[xj].Nodestatus == 3)||(TrainNew[xj].NextNode < TrainNew[i].NextNode)))//考虑区间！！！！！?????
                                                                { NI++; }                                                                
                                                            }
                                                        }
                                                    }

                                                    SI = TrainNew[i].NextNode - K1 + 1;//

                                                    if (NI <= SI || (TrainNew[i - 2].check == 1 && NI - 1 <= SI && TrainNew[i - 2].NextNode == K1-1))
                                                    //if (NI <= SI )
                                                    { TrainNew[i].check = 1; decide_num++; decideNum[i] = 1; }
                                                    else { TrainNew[i].check = 0; decide_num++; decideNum[i] = 1; }

                                                    if (i > 1)
                                                    {
                                                        if (TrainNew[i].NextNode == K - 2 && TrainNew[i - 2].NextNode == TrainNew[i].NextNode && TrainNew[i - 2].Nodestatus < 3) //起点的终极改变，decide_num不自加
                                                        { TrainNew[i].check = 0; }

                                                        if (TrainNew[i].NextNode == K - 2 && runtime - TrainNew[i - 2].departure[K - 1] < Headway)//考虑到发车的headway 间隔
                                                        { TrainNew[i].check = 0; }
                                                    }
                                                    
                                               }
                                            }

                                            else if (ljcount == 0)//如果不存在借用outbound的对向列车
                                            { 
                                                TrainNew[i].check = 1; decide_num++; decideNum[i] = 1;

                                                if (i > 1)
                                                {
                                                    if (TrainNew[i].NextNode == K - 2 && TrainNew[i - 2].NextNode == TrainNew[i].NextNode && TrainNew[i - 2].Nodestatus < 3) //起点的终极改变，decide_num不自加
                                                    { TrainNew[i].check = 0; }

                                                    if (TrainNew[i].NextNode == K - 2 && runtime - TrainNew[i - 2].departure[K - 1] < Headway)//考虑到发车的headway 间隔
                                                    { TrainNew[i].check = 0; }
                                                }
                                            }
                                        }//TrainNew[i].LockIndex == 0
                                    }
                                }

                                //不在节点上的,在区间的一律放行
                                if (TrainNew[i].Nodestatus == 1 || TrainNew[i].Nodestatus == 3)
                                { TrainNew[i].check = 1; decide_num++; decideNum[i] = 1; }
                            }
                            else if (TrainNew[i].bdeparture == 2)
                            { TrainNew[i].check = 0; decide_num++; decideNum[i] = 1;}                            
                        }
                    }
                }


                Debug.Assert(decide_num == N); //检测是否所有列车都被判定

                int DyTimeT0 = M; int DyTimeTr = M; //事故发生的起末时刻为一个离散事件
                //确定离散下一个离散事件                    
                {
                    //故障前
                    if (runtime < t0)
                    {
                        for (int i = 0; i < N; i++)
                        {
                            if (TrainNew[i].check == 1 || TrainNew[i].check == 2) //判定为可行或尚未出发
                            {
                                if (TrainNew[i].bdeparture == 0) //列车未出发
                                {
                                    if (TrainOr[i].trainType == 0)
                                    {
                                        if (i > 1) //非前两列车
                                        {
                                            Dy_time[i] = TrainOr[i].arrival[0] - runtime; decide++;
                                        }
                                        else if (i == 0 || i == 1)
                                        {
                                            Dy_time[i] = TrainOr[i].SubDwellTime[0]; decide++;
                                        }
                                    }
                                    else if (TrainOr[i].trainType == 1)
                                    {
                                        if (i > 1)
                                        { Dy_time[i] = TrainOr[i].arrival[K - 1] - runtime; decide++; }
                                        else if (i == 0 || i == 1)
                                        { Dy_time[i] = TrainOr[i].SubDwellTime[K - 1]; decide++; }

                                    }
                                }

                                else if (TrainNew[i].bdeparture == 1) //列车出发且尚未到到达终点
                                {
                                    if (TrainNew[i].trainType == 0)
                                    {
                                        if (TrainNew[i].Nodestatus == 1)
                                        {
                                            if (TrainNew[i].NextNode == -K)
                                            { Dy_time[i] = TrainOr[i].SubDwellTime[K - 1] - (runtime - TrainNew[i].arrival[K - 1]); }
                                            else { Dy_time[i] = TrainOr[i].SubDwellTime[TrainNew[i].NextNode - 1] - (runtime - TrainNew[i].arrival[TrainNew[i].NextNode - 1]); } 
                                            decide++; 
                                        }
                                        else if (TrainNew[i].Nodestatus == 2)
                                        {
                                            Dy_time[i] = TrainOr[i].sectiontime[TrainNew[i].NextNode - 1]; //arranged section trip time - used time
                                            decide++;
                                        }
                                        else if (TrainNew[i].Nodestatus == 3)
                                        {
                                            Dy_time[i] = Math.Max(0, TrainOr[i].sectiontime[TrainNew[i].NextNode - 1] - (runtime - TrainNew[i].departure[TrainNew[i].NextNode - 1])); //arranged section trip time - used time
                                            decide++;
                                        }
                                    }
                                    else if (TrainNew[i].trainType == 1)
                                    {
                                        if (TrainNew[i].Nodestatus == 1)
                                        {
                                            if (TrainNew[i].NextNode == -K)
                                            { Dy_time[i] = TrainOr[i].SubDwellTime[0] - (runtime - TrainNew[i].arrival[0]); }
                                            else { Dy_time[i] = TrainOr[i].SubDwellTime[TrainNew[i].NextNode + 1] - (runtime - TrainNew[i].arrival[TrainNew[i].NextNode + 1]); }
                                            decide++; 
                                        }
                                        else if (TrainNew[i].Nodestatus == 2)
                                        {
                                            Dy_time[i] = TrainOr[i].sectiontime[TrainNew[i].NextNode]; decide++;
                                        }
                                        else if (TrainNew[i].Nodestatus == 3)
                                        {
                                            Dy_time[i] = Math.Max(0, TrainOr[i].sectiontime[TrainNew[i].NextNode] - (runtime - TrainNew[i].departure[TrainNew[i].NextNode + 1]));
                                            decide++;
                                        }
                                    }
                                }

                                else if (TrainNew[i].bdeparture == 2) //列车到达终点已离开整个系统
                                {
                                    Dy_time[i] = M; decide++;
                                }
                            }
                            else if (TrainNew[i].check == 0)//判定为不可行，需要在站点等候
                            {
                                Dy_time[i] = M; decide++;
                            }
                        }
                        DyTimeT0 = t0 - runtime;
                    }

                    //故障中
                    else if (runtime >= t0 && runtime < tr)
                    {
                        for (int i = 0; i < N; i++)
                        {
                            if (TrainNew[i].check == 0) //判定为不可行
                            {
                                Dy_time[i] = M; decide++;
                            }

                            else if (TrainNew[i].check == 1) //判定为可行
                            {
                                if (TrainNew[i].trainType == 0)
                                {
                                    if (TrainNew[i].Nodestatus == 1)
                                    {
                                        if (TrainNew[i].NextNode == -K)
                                        { 
                                            Dy_time[i] = Math.Max(0, TrainOr[i].SubDwellTime[K - 1] - (runtime - TrainNew[i].arrival[K - 1]));
                                         }

                                        else
                                        {                                            
                                            Dy_time[i] = Math.Max(0, TrainOr[i].SubDwellTime[TrainNew[i].NextNode - 1] - (runtime - TrainNew[i].arrival[TrainNew[i].NextNode - 1]));                                            
                                        }
                                        decide++; 
                                    }

                                    else if (TrainNew[i].Nodestatus == 2)
                                    {
                                        Dy_time[i] = TrainOr[i].sectiontime[TrainNew[i].NextNode - 1];
                                        decide++;
                                    }

                                    else if (TrainNew[i].Nodestatus == 3)
                                    {
                                        Dy_time[i] = Math.Max(0, TrainOr[i].sectiontime[TrainNew[i].NextNode - 1] - (runtime - TrainNew[i].departure[TrainNew[i].NextNode - 1]));
                                        decide++;
                                    }
                                }
                                else if (TrainNew[i].trainType == 1)
                                {
                                    //故障段右边
                                    if (TrainNew[i].NextNode > K2)
                                    {
                                        if (TrainNew[i].Nodestatus == 1)
                                        {
                                            if (TrainNew[i].NextNode == -K)
                                            { Dy_time[i] = TrainOr[i].SubDwellTime[0] - (runtime - TrainNew[i].arrival[0]); }
                                            else 
                                            { 
                                                Dy_time[i] = Math.Max(0, TrainOr[i].SubDwellTime[TrainNew[i].NextNode + 1] - (runtime - TrainNew[i].arrival[TrainNew[i].NextNode + 1]));
                                              
                                            }
                                            decide++; 
                                        }
                                        else if (TrainNew[i].Nodestatus == 2)
                                        {
                                            Dy_time[i] = TrainOr[i].sectiontime[TrainNew[i].NextNode];
                                            decide++;
                                        }
                                        else if (TrainNew[i].Nodestatus == 3)
                                        {
                                            Dy_time[i] = Math.Max(0, TrainOr[i].sectiontime[TrainNew[i].NextNode] - (runtime - TrainNew[i].departure[TrainNew[i].NextNode + 1]));
                                            decide++;
                                        }
                                    }
                                    //故障段K2+1
                                    else if (TrainNew[i].NextNode == K2) //要考虑渡线的时间
                                    {
                                        if (TrainNew[i].Nodestatus == 1)
                                        {
                                            Dy_time[i] = Math.Max(0, TrainOr[i].SubDwellTime[TrainNew[i].NextNode + 1] - (runtime - TrainNew[i].arrival[TrainNew[i].NextNode + 1])); //如果列车在站间等待时间过长，那么当判定为可行时，departuretime 为runtime
                                            decide++;
                                        }
                                        else if (TrainNew[i].Nodestatus == 2)
                                        {
                                            Dy_time[i] = TrainOr[i].sectiontime[TrainNew[i].NextNode] + tc;
                                            decide++;
                                        }
                                        else if (TrainNew[i].Nodestatus == 3)
                                        {
                                            Dy_time[i] = Math.Max(0, TrainOr[i].sectiontime[TrainNew[i].NextNode] + tc - (runtime - TrainNew[i].departure[TrainNew[i].NextNode + 1]));
                                            decide++;
                                        }
                                    }
                                    //故障段中
                                    else if (TrainNew[i].NextNode < K2 && TrainNew[i].NextNode >= K1)
                                    {
                                        if (TrainNew[i].Nodestatus == 1)
                                        {
                                            Dy_time[i] = Math.Max(0, TrainOr[i].SubDwellTime[TrainNew[i].NextNode + 1] - (runtime - TrainNew[i].arrival[TrainNew[i].NextNode + 1]));
                                            decide++;
                                        }
                                        else if (TrainNew[i].Nodestatus == 2)
                                        {
                                            Dy_time[i] = TrainOr[i].sectiontime[TrainNew[i].NextNode];
                                            decide++;
                                        }
                                        else if (TrainNew[i].Nodestatus == 3)
                                        {
                                            Dy_time[i] = Math.Max(0, TrainOr[i].sectiontime[TrainNew[i].NextNode] - (runtime - TrainNew[i].departure[TrainNew[i].NextNode + 1]));
                                            decide++;
                                        }
                                    }

                                    else if (TrainNew[i].NextNode == K1-1)//要考虑渡线的时间
                                    {
                                        if (TrainNew[i].Nodestatus == 1)
                                        {
                                            //Dy_time[i] = TrainOr[i].DwellTime[TrainNew[i].NextNode + 1] - (runtime - TrainNew[i].arrival[TrainNew[i].NextNode + 1]);
                                            Dy_time[i] = Math.Max(0, TrainOr[i].SubDwellTime[TrainNew[i].NextNode + 1] - (runtime - TrainNew[i].arrival[TrainNew[i].NextNode + 1]));
                                            decide++;                                            
                                        }
                                        else if (TrainNew[i].Nodestatus == 2)
                                        {
                                            Dy_time[i] = TrainOr[i].sectiontime[TrainNew[i].NextNode] + tc;
                                            decide++;
                                        }
                                        else if (TrainNew[i].Nodestatus == 3)
                                        {
                                            Dy_time[i] = Math.Max(0, TrainOr[i].sectiontime[TrainNew[i].NextNode] + tc - (runtime - TrainNew[i].departure[TrainNew[i].NextNode + 1]));
                                            decide++;
                                        }
                                    }
                                    //故障段左边
                                    else if (TrainNew[i].NextNode < K1-1)
                                    {
                                        if (TrainNew[i].Nodestatus == 1)
                                        {
                                            if (TrainNew[i].NextNode == -K)
                                            { Dy_time[i] = TrainOr[i].SubDwellTime[0] - (runtime - TrainNew[i].arrival[0]); }
                                            else { Dy_time[i] = TrainOr[i].SubDwellTime[TrainNew[i].NextNode + 1] - (runtime - TrainNew[i].arrival[TrainNew[i].NextNode + 1]); }
                                            decide++; 
                                        }
                                        else if (TrainNew[i].Nodestatus == 2)
                                        {
                                            Dy_time[i] = TrainOr[i].sectiontime[TrainNew[i].NextNode];
                                            decide++;
                                        }
                                        else if (TrainNew[i].Nodestatus == 3)
                                        {
                                            Dy_time[i] = Math.Max(0, TrainOr[i].sectiontime[TrainNew[i].NextNode] - (runtime - TrainNew[i].departure[TrainNew[i].NextNode + 1]));
                                            decide++;
                                        }
                                    }
                                }
                            }

                            else if (TrainNew[i].check == 2) //列车未出发//判定为最高优先权
                            {
                                if (TrainNew[i].bdeparture == 0)
                                {
                                    if (TrainOr[i].trainType == 0)
                                    {
                                        if (i > 1) //非前两列车
                                        {
                                            Dy_time[i] = TrainOr[i].arrival[0] - runtime; decide++;
                                        }
                                        else if (i == 0 || i == 1)
                                        {
                                            Dy_time[i] = TrainOr[i].SubDwellTime[0]; decide++;
                                        }
                                    }
                                    else if (TrainOr[i].trainType == 1)
                                    {
                                        if (i > 1)
                                        { Dy_time[i] = TrainOr[i].arrival[K - 1] - runtime; decide++; }
                                        else if (i == 0 || i == 1)
                                        { Dy_time[i] = TrainOr[i].SubDwellTime[K - 2]; decide++; }
                                    }
                                }
                            }
                        }

                        DyTimeTr = tr - runtime;
                    }

                    //故障后
                    else if (runtime >= tr)
                    {
                        for (int i = 0; i < N; i++)
                        {
                            if (TrainNew[i].check == 0)
                            {
                                Dy_time[i] = M; decide++; 
                            }

                            else if (TrainNew[i].check == 1)
                            {
                                if (TrainNew[i].trainType == 0)
                                {
                                    if (TrainNew[i].Nodestatus == 1)
                                    {
                                        if (TrainNew[i].NextNode == -K)
                                        { Dy_time[i] = Math.Max(0, TrainOr[i].SubDwellTime[K - 1] - (runtime - TrainNew[i].arrival[K - 1])); } 

                                        else
                                        {
                                            Dy_time[i] = Math.Max(0, TrainOr[i].SubDwellTime[TrainNew[i].NextNode - 1] - (runtime - TrainNew[i].arrival[TrainNew[i].NextNode - 1]));
                                        }
                                        decide++; 
                                    }

                                    else if (TrainNew[i].Nodestatus == 2)
                                    {
                                        Dy_time[i] = TrainOr[i].sectiontime[TrainNew[i].NextNode - 1];
                                        decide++;
                                    }

                                    else if (TrainNew[i].Nodestatus == 3)
                                    {
                                        Dy_time[i] = Math.Max(0, TrainOr[i].sectiontime[TrainNew[i].NextNode - 1] - (runtime - TrainNew[i].departure[TrainNew[i].NextNode - 1]));
                                        decide++;
                                    }
                                }
                                else if (TrainNew[i].trainType == 1)
                                {
                                    if (TrainNew[i].Nodestatus == 1)
                                    {
                                        if (TrainNew[i].NextNode == -K)
                                        { Dy_time[i] = TrainOr[i].SubDwellTime[0] - (runtime - TrainNew[i].arrival[0]); }
                                        else { Dy_time[i] = Math.Max(0, TrainOr[i].SubDwellTime[TrainNew[i].NextNode + 1] - (runtime - TrainNew[i].arrival[TrainNew[i].NextNode + 1])); }
                                        decide++;
                                    }
                                    else if (TrainNew[i].Nodestatus == 2)
                                    {
                                        if (TrainNew[i].NextNode == K1 - 1 && TrainNew[i].PathType == 0)//借用outbound区间的列车
                                        {
                                            Dy_time[i] = TrainOr[i].sectiontime[TrainNew[i].NextNode] + tc;
                                            decide++;
                                        }
                                        else
                                        {
                                            Dy_time[i] = TrainOr[i].sectiontime[TrainNew[i].NextNode];
                                            decide++;
                                        }
                                    }
                                    else if (TrainNew[i].Nodestatus == 3)
                                    {
                                        if (TrainNew[i].NextNode == K1 - 1  && TrainNew[i].PathType == 0)
                                        {
                                            Dy_time[i] = Math.Max(0, TrainOr[i].sectiontime[TrainNew[i].NextNode] + tc - (runtime - TrainNew[i].departure[TrainNew[i].NextNode + 1]));
                                            decide++;
                                        }
                                        else
                                        {
                                            Dy_time[i] = Math.Max(0, TrainOr[i].sectiontime[TrainNew[i].NextNode] - (runtime - TrainNew[i].departure[TrainNew[i].NextNode + 1]));
                                            decide++;
                                        }
                                    }                                    
                                }
                            }

                            else if (TrainNew[i].check == 2)
                            {
                                if (TrainNew[i].bdeparture == 0)
                                {
                                    if (TrainOr[i].trainType == 0)
                                    {
                                        if (i > 1) //非前两列车
                                        {
                                            Dy_time[i] = TrainOr[i].arrival[0] - runtime; decide++;
                                        }
                                        else if (i == 0 || i == 1)
                                        {
                                            Dy_time[i] = TrainOr[i].SubDwellTime[0]; decide++;
                                        }
                                    }
                                    else if (TrainOr[i].trainType == 1)
                                    {
                                        if (i > 1)
                                        { Dy_time[i] = TrainOr[i].arrival[K - 1] - runtime; decide++; }
                                        else if (i == 0 || i == 1)
                                        { Dy_time[i] = TrainOr[i].SubDwellTime[K - 2]; decide++; }

                                    }
                                }
                            }
                        }
                    }
                }//确定离散下一个离散事件  

                //找出最小Dy_Time  //有N+2个选择
                Debug.Assert(decide == N);
                if (decide == N)
                {
                    Dy_Time = Math.Min(DyTimeT0, M);
                    Dy_Time = Math.Min(DyTimeTr, Dy_Time);
                    for (int i = 0; i < N; i++)
                    { Dy_Time = Math.Min(Dy_Time, Dy_time[i]); }
                }
                Dy_Time = Dy_Time + 0;  //Dy_Time<0
                Debug.Assert(Dy_Time >= 0);

                int decideUpdate = 0; int[] DecideUpdate = new int[N];
                for (int i = 0; i < N; i++) 
                {
                    DecideUpdate[i] = -1;
                }
                //更新列车列车信息（timetable）
                {
                    if (runtime < TT) //if (runtime < t0)
                    {
                        for (int i = 0; i < N; i++)
                        {
                            if (TrainNew[i].check == 0) //列车被禁行，所有信息不变
                            {
                                TrainNew[i].time = runtime + Dy_Time;
                                decideUpdate++; DecideUpdate[i] = 0;
                            }

                            else if (TrainNew[i].check == 1)
                            {
                                if (Dy_Time == Dy_time[i])
                                {
                                    if (TrainNew[i].trainType == 0)
                                    {
                                        if (TrainNew[i].Nodestatus == 1) //此时列车完成dwell time 的任务，将nodestatus 改变，其他基本不变
                                        {

                                            TrainNew[i].time = runtime + Dy_Time;
                                            TrainNew[i].Nodestatus = 2;

                                            if (TrainNew[i].NextNode == -K)//此站为终点站
                                            {
                                                TrainNew[i].departure[K - 1] = runtime + Dy_Time;
                                                TrainNew[i].bdeparture = 2;
                                            }
                                            else
                                            {
                                                TrainNew[i].departure[TrainNew[i].NextNode - 1] = runtime + Dy_Time;
                                                TrainNew[i].bdeparture = 1;
                                            }
                                            decideUpdate++; DecideUpdate[i] = 0;
                                        }
                                        else if (TrainNew[i].Nodestatus == 2) //此时列车当出发，此步确定departure time其下一步的状态：nodestatus是1，nextnode+1，
                                        {
                                            TrainNew[i].time = runtime + Dy_Time;
                                            TrainNew[i].Nodestatus = 1;
                                            TrainNew[i].departure[TrainNew[i].NextNode - 1] = runtime;
                                            TrainNew[i].arrival[TrainNew[i].NextNode] = runtime + Dy_Time;
                                            if (TrainNew[i].NextNode == K - 1)
                                            { TrainNew[i].NextNode = -K; }
                                            else
                                            { TrainNew[i].NextNode = TrainNew[i].NextNode + 1; }
                                            decideUpdate++; DecideUpdate[i] = 0;
                                        }
                                        else if (TrainNew[i].Nodestatus == 3) //此时列车要进站，其下一步的状态 nodestatus为1，arrival time可确定
                                        {
                                            TrainNew[i].time = runtime + Dy_Time;
                                            TrainNew[i].Nodestatus = 1;
                                            TrainNew[i].arrival[TrainNew[i].NextNode] = runtime + Dy_Time;                                           
                                            if (TrainNew[i].NextNode == K - 1)
                                            { TrainNew[i].NextNode = -K; }
                                            else
                                            { TrainNew[i].NextNode = TrainNew[i].NextNode + 1; }
                                            decideUpdate++; DecideUpdate[i] = 0;
                                        }
                                    }
                                    else if (TrainNew[i].trainType == 1)
                                    {
                                        if (TrainNew[i].Nodestatus == 1) //此时列车完成dwell time 的任务，将nodestatus 改变，其他基本不变
                                        {
                                            TrainNew[i].time = runtime + Dy_Time;
                                            TrainNew[i].Nodestatus = 2;
                                            if (TrainNew[i].NextNode == -K)//此站为终点站
                                            {
                                                TrainNew[i].departure[0] = runtime + Dy_Time;
                                                TrainNew[i].bdeparture = 2;
                                            }
                                            else
                                            {
                                                TrainNew[i].departure[TrainNew[i].NextNode + 1] = runtime + Dy_Time;
                                                TrainNew[i].bdeparture = 1;
                                            }
                                            decideUpdate++; DecideUpdate[i] = 0;
                                        }
                                        else if (TrainNew[i].Nodestatus == 2)
                                        {
                                            TrainNew[i].time = runtime + Dy_Time;
                                            TrainNew[i].Nodestatus = 1;
                                            TrainNew[i].departure[TrainNew[i].NextNode + 1] = runtime;
                                            TrainNew[i].arrival[TrainNew[i].NextNode] = runtime + Dy_Time;

                                            if (TrainNew[i].NextNode == K1 - 1)
                                            {
                                                if (TrainNew[i].PathType == 0) { TrainNew[i].PathType = 1; } //Inbound 列车返回原来的轨道

                                                if (TrainNew[i].LockIndex == 1) { TrainNew[i].LockIndex = 0; }//解除锁定
                                            }

                                            if (TrainNew[i].NextNode == 0)
                                            { TrainNew[i].NextNode = -K; }
                                            else
                                            { TrainNew[i].NextNode = TrainNew[i].NextNode - 1; }
                                            decideUpdate++; DecideUpdate[i] = 0;
                                        }
                                        else if (TrainNew[i].Nodestatus == 3)
                                        {
                                            TrainNew[i].time = runtime + Dy_Time;
                                            TrainNew[i].Nodestatus = 1;
                                            TrainNew[i].arrival[TrainNew[i].NextNode] = runtime + Dy_Time;

                                            if (TrainNew[i].NextNode == K1 - 1)
                                            {
                                                if (TrainNew[i].PathType == 0) { TrainNew[i].PathType = 1; } //Inbound 列车返回原来的轨道

                                                if (TrainNew[i].LockIndex == 1) { TrainNew[i].LockIndex = 0; }//解除锁定
                                            }

                                            if (TrainNew[i].NextNode == 0)
                                            { TrainNew[i].NextNode = -K; }
                                            else
                                            { TrainNew[i].NextNode = TrainNew[i].NextNode - 1; }
                                            decideUpdate++; DecideUpdate[i] = 0;
                                        }
                                    }
                                }
                                else if (Dy_Time < Dy_time[i]) //状态不会改变
                                {
                                    if (TrainNew[i].trainType == 0)
                                    {
                                        if (TrainNew[i].Nodestatus == 1)
                                        { TrainNew[i].time = runtime + Dy_Time; decideUpdate++; DecideUpdate[i] = 0; }
                                        else if (TrainNew[i].Nodestatus == 2)
                                        { 
                                            TrainNew[i].time = runtime + Dy_Time;

                                            TrainNew[i].departure[TrainNew[i].NextNode - 1] = runtime;//--

                                            TrainNew[i].Nodestatus = 3; 
                                            decideUpdate++; DecideUpdate[i] = 0; 
                                        }
                                        else if (TrainNew[i].Nodestatus == 3)
                                        { TrainNew[i].time = runtime + Dy_Time; decideUpdate++; DecideUpdate[i] = 0; }
                                    }
                                    else if (TrainNew[i].trainType == 1)
                                    {
                                        if (TrainNew[i].Nodestatus == 1)
                                        { TrainNew[i].time = runtime + Dy_Time; decideUpdate++; DecideUpdate[i] = 0; }
                                        else if (TrainNew[i].Nodestatus == 2)
                                        { 
                                            TrainNew[i].time = runtime + Dy_Time;
                                            if (TrainNew[i].NextNode == K1)
                                            {
                                                if (TrainNew[i].PathType == 0) { TrainNew[i].PathType = 1; } //Inbound 列车返回原来的轨道

                                                if (TrainNew[i].LockIndex == 1) { TrainNew[i].LockIndex = 0; }//解除锁定
                                            }

                                            TrainNew[i].departure[TrainNew[i].NextNode + 1] = runtime;//--

                                            TrainNew[i].Nodestatus = 3; 
                                            decideUpdate++; DecideUpdate[i] = 0; 
                                        }
                                        else if (TrainNew[i].Nodestatus == 3)
                                        { TrainNew[i].time = runtime + Dy_Time; decideUpdate++; DecideUpdate[i] = 0; }
                                    }
                                }
                            }

                            else if (TrainNew[i].check == 2) //列车为出发而待出发
                            {
                                if (Dy_Time == Dy_time[i]) //如果离散事件是由为出发的列车出站引起的，对将要出站的列车进行状态改变
                                {
                                    if (TrainNew[i].trainType == 0)
                                    {
                                        if (i == 0)
                                        {
                                            TrainNew[i].time = runtime + Dy_Time;
                                            TrainNew[i].arrival[0] = runtime;
                                            TrainNew[i].departure[0] = runtime + Dy_Time;
                                            TrainNew[i].bdeparture = 1;
                                            TrainNew[i].NextNode = TrainNew[i].NextNode + 1;
                                            TrainNew[i].Nodestatus = 2;
                                            decideUpdate++; DecideUpdate[i] = 0;
                                        }
                                        else
                                        {
                                            TrainNew[i].time = runtime + Dy_Time;
                                            TrainNew[i].arrival[0] = runtime + Dy_Time;
                                            TrainNew[i].bdeparture = 1;
                                            TrainNew[i].Nodestatus = 1;
                                            TrainNew[i].NextNode = TrainNew[i].NextNode + 1;
                                            decideUpdate++; DecideUpdate[i] = 0;
                                        }
                                    }
                                    else if (TrainNew[i].trainType == 1)
                                    {
                                        if (i == 1)
                                        {
                                            TrainNew[i].time = runtime + Dy_Time;
                                            TrainNew[i].arrival[K - 1] = runtime;
                                            TrainNew[i].departure[K - 1] = runtime + Dy_Time;
                                            TrainNew[i].bdeparture = 1;
                                            TrainNew[i].NextNode = TrainNew[i].NextNode - 1;
                                            TrainNew[i].Nodestatus = 2;
                                            decideUpdate++; DecideUpdate[i] = 0;
                                        }

                                        else
                                        {
                                            TrainNew[i].time = runtime + Dy_Time;
                                            TrainNew[i].arrival[K - 1] = runtime + Dy_Time;
                                            TrainNew[i].bdeparture = 1;
                                            TrainNew[i].NextNode = TrainNew[i].NextNode - 1;
                                            TrainNew[i].Nodestatus = 1;
                                            decideUpdate++; DecideUpdate[i] = 0;
                                        }
                                    }
                                }
                                else if (Dy_Time < Dy_time[i])
                                {                          
                                    if (TrainNew[i].trainType == 0)
                                    {
                                        if (runtime <= (i / 2) * Headway && runtime + Dy_Time > (i / 2) * Headway)
                                        {
                                            TrainNew[i].time = runtime + Dy_Time;
                                            TrainNew[i].arrival[0] = runtime;
                                            TrainNew[i].bdeparture = 1;
                                            TrainNew[i].NextNode = TrainNew[i].NextNode + 1;
                                            TrainNew[i].Nodestatus = 1;
                                        }
                                        else
                                        {
                                            TrainNew[i].time = runtime + Dy_Time;
                                            TrainNew[i].bdeparture = 0;
                                        }
                                        decideUpdate++; DecideUpdate[i] = 0;
                                    }
                                    else if (TrainNew[i].trainType == 1)
                                    {
                                        if (runtime <= ((i - 1) / 2) * Headway && runtime + Dy_Time > ((i - 1) / 2) * Headway)
                                        {
                                            TrainNew[i].time = runtime + Dy_Time;
                                            TrainNew[i].arrival[K - 1] = runtime;
                                            TrainNew[i].bdeparture = 1;
                                            TrainNew[i].NextNode = TrainNew[i].NextNode - 1;
                                            TrainNew[i].Nodestatus = 1;
                                        }
                                        else
                                        {
                                            TrainNew[i].time = runtime + Dy_Time;
                                            TrainNew[i].bdeparture = 0;
                                        }
                                        decideUpdate++; DecideUpdate[i] = 0;
                                    }
                                } //如果不是，则不需要做任何改变
                            }
                        }
                    }            


                } //更新列车列车信息（timetable）

                Debug.Assert(decideUpdate == N);
                //延迟信息统计，输出数据

                runtime = runtime + Dy_Time;
                DynamicCount++;
            }

            sw.Stop();
            
            int SysTime;
            SysTime = Int32.Parse( sw.ElapsedMilliseconds.ToString());

            //延迟信息统计，输出数据
            int[] Delay = new int[N];
            int DelaySum = 0;
            for (int i = 0; i < N; i++) 
            {
                if (TrainNew[i].trainType == 0)
                {
                    Delay[i] = TrainNew[i].departure[K - 1] - TrainOr[i].departure[K - 1]; 
                    DelaySum = DelaySum + Delay[i]; 
                }
                else if (TrainNew[i].trainType == 1) 
                {
                    Delay[i] = TrainNew[i].departure[0] - TrainOr[i].departure[0];
                    DelaySum = DelaySum + Delay[i];
                }
            }

            int ClearTime = 0;
            ClearTime = Math.Max(TrainNew[N - 2].departure[K - 1], TrainNew[N - 1].departure[0]);
            int ClearTimeOR = 0;
            ClearTimeOR = Math.Max(TrainOr[N - 2].departure[K - 1], TrainOr[N - 1].departure[0]);
            int ClearTimeDelay = 0;
            ClearTimeDelay = ClearTime - ClearTimeOR;

            int maxdelay = 0; int[] MaxdelayID = new int[N]; int MaxDeIdCount = 0; int DeTrainSum = 0;
            for (int i = 0; i < N; i++) 
            {
                if (Delay[i] > 0) { DeTrainSum++; }

                maxdelay = Math.Max(maxdelay, Delay[i]);
            }

            for (int i = 0; i < N; i++)
            {
                if (maxdelay == Delay[i])
                {
                    MaxdelayID[MaxDeIdCount] = i;
                    MaxDeIdCount++;
                }
            }

            outdata.WriteLine("事故开始时间： " + t0 + ", 事故结束时间：" + tr + "， Headway 为： " + Headway + ".");
            for (int i = 0; i < N; i++)
            {
                //outdata.WriteLine("Delay time of train " + (i + 1) + " is: " + Delay[i] + " Second, and the timetable of Train " + (i + 1) + " is: ");
                outdata.WriteLine("Train: " + (i + 1));
                if (TrainNew[i].trainType == 0)
                {
                    for (int k = 0; k < K; k++)
                    {
                        outdata.Write(TrainNew[i].arrival[k] + " " + TrainNew[i].departure[k] + " ");
                    }
                }
                else if (TrainNew[i].trainType == 1)
                {
                    for (int k = 0; k < K; k++)
                    {
                        outdata.Write(TrainNew[i].departure[k] + " " + TrainNew[i].arrival[k] + " ");
                    }
                }
                outdata.WriteLine();
            }
            outdata.WriteLine();
            for (int i = 0; i < N; i++)
            {
                outdata.WriteLine("Delay time of train " + (i + 1) + " is: " + Delay[i] + " Seconds;");
            }
            outdata.WriteLine();
            outdata.WriteLine("The sum delay time is: " + DelaySum + " Seconds," + " i.e., " + DelaySum / 60 + " min or " + (float) DelaySum / 3600 + " hours.");
            outdata.WriteLine("受影响的列车有： " + DeTrainSum + " 辆列车，其中最大延迟时间是： " + maxdelay + " Seconds, 为如下列车：");
            for (int i = 0; i < MaxDeIdCount; i++) 
            {
                outdata.Write("Train: " + (MaxdelayID[i] + 1) + ", ");
            }
            outdata.WriteLine();
            outdata.WriteLine("原始的线路清空时间: " + ClearTimeOR + " Seconds," + " i.e., " + ClearTimeOR / 60 + " min or " + (float)ClearTimeOR / 3600 + " hours.");
            outdata.WriteLine("调整图的线路清空时间: " + ClearTime + " Seconds," + " i.e., " + ClearTime / 60 + " min or " + (float)ClearTime / 3600 + " hours.");
            outdata.WriteLine("线路清空时间延迟: " + ClearTimeDelay + " Seconds," + " i.e., " + ClearTimeDelay / 60 + " min or " + (float)ClearTimeDelay / 3600 + " hours.");
            outdata.WriteLine("CPU time is " + sw.ElapsedMilliseconds.ToString() + " milliseconds" + ", i.e., " + SysTime / 1000 + " seconds");
            outdata.Close();
            
        }
    }
}

//DataInput;输入信息：车辆数N；故障发生时间；故障结束时间；区间数；站台数；发生故障区间；原始的timetable
//DetermineInfo;定位函数：依据上述信息，迅速定位每列车所在的区间或者位置（是否出发或到达）输出含有相关信息的Train类

//Train 类：time;type(In/Out);path[k]

    /*
public class CTrain
{
    public CTrain(int K) 
    {
        this.arrival = new int[K];
        this.departure = new int[K];
        this.DwellTime = new int[K];
        this.SectionTime = new int[K - 1];
    }
    public double position;
    public double velocity;
    public double time;
    public int NextNode;
    public int[] arrival;
    public int[] departure;
    public int[] SectionTime;
    public int[] DwellTime;
    public int NodeStatus;//0-列车不在线上（未出发或者已到达终点），1-列车处于刚到站状态，2-列车处于待出发状态,3-列车在区间上
    public int bDeparture; //出发标记，=0，未出发，1-已出发且尚未到达终点，2-已到达终点
    public int type;  //train is inbound(=1) or outbound(=0)?
    public int PathType; //0-outbound; 1-inbound
    public int LockIndex; //1-locked; 0-no locked
    public int check; //列车决策变量；1-通行；0-不通行;2-即将发车，为最高优先权
}
*/

public class CPV
{
    public CPV(int S) //参数为距离
    {
        this.pos = new double[S];
        this.vel = new double[S];
        this.mt = new double[S];
        this.me = new double[S];

    }
    public int S;
    public double[] pos;            //假设对应区间长度最大为3万米，则对应3万个数
    public double[] vel;
    public double[] mt; //每米所用时间
    public double[] me; //每米所用能耗
}


//public static CTrain Trainor(int InA[], )
//{

//}


//BeforeSch;故障发生前:列车路径确定，不受影响的列车按照原计划排定


//IncidentSch;故障发生时：对受到影响的上行列车进行列车路径更新，进行有单线的GA调度


//AfterSch;故障发生后：重新更新列车路径


//DynamicDet;离散事件的确立
   