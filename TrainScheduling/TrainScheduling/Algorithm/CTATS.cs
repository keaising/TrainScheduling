using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrainScheduling.Model;

namespace TrainScheduling.Algorithm
{
    public class CTATS
    {
        public CTATS(List<CTrain> train, List<CRailwayStation> station, List<CRailwaySection> section, int _nset)
        {
            bool complete = false;
            int _ntrain = train.Count, _nstation = station.Count;// _nsection=section.Count;
            int systime = 0;

            CParameter parameter = new CParameter();
            CInitialize_Information initialize = new CInitialize_Information(train, station, section);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (!complete) //if the process is completed, break
            {
                int[] Dy_Time = new int[_ntrain]; //discrete time of each train
                int Count_complete_journey = new int();// discrete time of system
                for (int i = 0; i < _ntrain; i++)
                    train[i].check = 0; //check status   

                systime = train[0].time;

                Capacity_Check(train, station, systime);

                Discrete_Definition(train, systime, Dy_Time);

                Update_Information(train, systime, Dy_Time);

                for (int i = 0; i < train.Count; i++)
                    if (train[i].bdeparture == 2)
                        Count_complete_journey++;
                if (Count_complete_journey == train.Count)
                    complete = true;
            }
            sw.Stop();
            int ITAS_cpu_Time = Int32.Parse(sw.ElapsedMilliseconds.ToString()); // unit is msec
            COutput_ITAS_Result output_result = new COutput_ITAS_Result(train, station, section, _nset);

            double ITAS_UB = 0;
            for (int i = 0; i < _ntrain; i++)
            {
                train[i].DelayTime = train[i].departure[train[i].route[train[i].route.Count - 1]] - train[i].free_departure[train[i].route[train[i].route.Count - 1]];
                ITAS_UB = ITAS_UB + train[i].DelayTime;
            }
            //ITAS_outpu.WriteLine(_nset + "\t" + train.Count + "\t" + ITAS_UB + "\t" + ITAS_cpu_Time);
        }

        public void Capacity_Check(List<CTrain> train, List<CRailwayStation> station, int systime) //systime is the system time, unit is sec
        {
            //能力检测算法   1-通行；0-不通行;2-即将发车，为最高优先权
            //nodestatus 0-out the system, 1-just arrive a station(列车刚到站状态), 2-prepare to leave the staion(列车处于待出发状态)，3-on the section(列车在区间上)
            int _ntrain = train.Count, _nstation = station.Count;
            int _ndecide = 0; bool[] decideNum = new bool[train.Count];
            for (int i = 0; i < _ntrain; i++)
                decideNum[i] = false;

            for (int i = 0; i < _ntrain; i++)
            {
                //------------ train i don't arrives in the system, 未出发列车 ---------------//
                if (train[i].bdeparture == 0)
                {
                    if (train[i].arrival[train[i].route[0]] == systime)  //train i should depart from the origin station at this time
                    { train[i].check = 1; decideNum[i] = true; _ndecide++; }
                    else if (train[i].arrival[train[i].route[0]] > systime)    //train i don't need to depart from the origin station at this time
                    { train[i].check = 0; decideNum[i] = true; _ndecide++; }
                }

                //--------------  train i in the system -------------//
                if (train[i].bdeparture == 1) //train i in the system
                {
                    //prepare to leave the current station
                    if (train[i].Nodestatus == 2) //
                    {
                        train[i].check = check_capacity_LF(train, systime, i);

                        if (train[i].CurRouteNode < train[i].route.Count - 1)
                            train[i].check = fast_check_new(train, systime, i);

                        decideNum[i] = true; _ndecide++;
                    }

                    //just arrives at the station or dwelling time < planed one; or on the section 
                    else if (train[i].Nodestatus == 1 || train[i].Nodestatus == 3)
                    { train[i].check = 1; decideNum[i] = true; _ndecide++; }
                }

                else if (train[i].bdeparture == 2) // train i leaves the system
                { train[i].check = 0; _ndecide++; decideNum[i] = true; }

            }
            Debug.Assert(_ndecide == train.Count); //check whether all the trains have been checked 
        }

        internal int check_capacity_LF(List<CTrain> train, int systime, int i)
        {
            int N_O = 1, N_I = 0, S_O = 1, S_I = 0;
            bool check_flag1 = true, check_flag2 = true;

            if (train[i].CurRouteNode == train[i].route.Count - 1) //train i dwells at its destination
            { train[i].check = 1; }
            else
            {
                for (int j = train[i].CurRouteNode; j < train[i].route.Count - 1; j++)
                {
                    int m_O_s = 0, m_O_r1 = 0; // r current station, s next section, r1 next station, s1 next next station
                    int m_I_s = 0, m_I_r1 = 0; // r current station, s next section, r1 next station, s1 next next station

                    //look at the next section
                    for (int k = 0; k < train.Count; k++)
                        if (train[k].bdeparture == 1 && k != i)
                        {
                            if (train[k].trainType == train[i].trainType) // in the same direction 
                                if (train[k].route[train[k].CurRouteNode] == train[i].route[j] && train[k].Nodestatus == 3) //on the next section
                                { m_O_s++; }

                            if (train[k].trainType != train[i].trainType) // in different directions
                                if (train[k].route[train[k].CurRouteNode] == train[i].route[j + 1] && train[k].Nodestatus == 3) //on the next section
                                { m_I_s++; }
                        }
                    N_O = N_O + m_O_s; N_I = N_I + m_I_s;

                    //check step 1
                    if (m_I_s > 0)
                    {
                        int m_same = 1, m_opps = 0;
                        //look back all stations and sections 
                        for (int n = train[i].CurRouteNode; n <= j; n++)
                        {
                            for (int k = 0; k < train.Count; k++)
                                if (train[k].bdeparture == 1 && k != i)
                                {
                                    if (train[k].trainType == train[i].trainType) // in the same direction 
                                        if (train[k].route[train[k].CurRouteNode] == train[i].route[n]) //same direction trains on section or station                                          
                                            //if (!(n == train[i].CurRouteNode && train[k].check == 0 && train[k].Nodestatus < 3)) { m_same++; }
                                            if (!(n == train[i].CurRouteNode && train[k].check == 0 && train[k].Nodestatus < 3)) { m_same++; }

                                    if (train[k].trainType != train[i].trainType) // in different directions
                                        if (train[k].route[train[k].CurRouteNode] == train[i].route[n + 1] && train[k].Nodestatus == 3) //different directions trains on section or station
                                        //if (!(n + 1 == train[i].route.Count - 1)) 
                                        { m_opps++; }
                                }
                        }
                        if (m_same > S_O || m_opps > S_I)
                        { train[i].check = 0; check_flag1 = false; break; }
                    }

                    //look the next station
                    for (int k = 0; k < train.Count; k++)
                        if (train[k].bdeparture == 1 && k != i)
                        {
                            if (train[k].trainType == train[i].trainType) // in the same direction 
                                if (train[k].route[train[k].CurRouteNode] == train[i].route[j + 1] && train[k].Nodestatus < 3) //on the next station
                                { m_O_r1++; }

                            if (train[k].trainType != train[i].trainType) // in different directions
                                if (train[k].route[train[k].CurRouteNode] == train[i].route[j + 1] && train[k].Nodestatus < 3) //on the next station
                                { m_I_r1++; }
                        }

                    //check step 2
                    if (m_I_r1 > 0)
                    {
                        int m_same = 0; int m_opps = 0;
                        for (int n = train[i].CurRouteNode; n <= j + 1; n++)
                        {
                            for (int k = 0; k < train.Count; k++)
                                if (train[k].bdeparture == 1 && k != i)
                                {
                                    if (train[k].trainType == train[i].trainType && train[k].route[train[k].CurRouteNode] == train[i].route[n])
                                        //if (train[k].check == 0) //something rong
                                        if (!(train[k].check == 1 && train[k].route[train[k].CurRouteNode] == train[i].route[0])) { m_same++; }

                                    if (train[k].trainType != train[i].trainType && train[k].route[train[k].CurRouteNode] == train[i].route[n])
                                    { m_opps++; }
                                }
                        }
                        if (m_same > S_O && m_opps > 0)
                        { train[i].check = 0; check_flag2 = false; break; }
                    }
                    //if (m_I_r == 0 && m_O_r == 0) { S_O++; S_I++; }
                    //else if (m_I_r == 0 && m_O_r != 0) { S_I++; }
                    //else if (m_I_r != 0 && m_O_r == 0) { S_O++; }

                    if (m_I_r1 == 0 && m_O_r1 == 0) { S_O++; S_I++; S_O++; S_I++; }
                    else if (m_I_r1 == 0 && m_O_r1 == 1) { S_I++; S_I++; S_O++; }
                    else if (m_I_r1 == 0 && m_O_r1 == 2) { S_I++; S_I++; }
                    else if (m_I_r1 == 1 && m_O_r1 == 0) { S_O++; S_O++; S_I++; }
                    else if (m_I_r1 == 1 && m_O_r1 == 1) { S_O++; S_I++; }
                    else if (m_I_r1 == 1 && m_O_r1 == 2) { S_I++; }
                    else if (m_I_r1 == 2 && m_O_r1 == 0) { S_O++; S_O++; }
                    else if (m_I_r1 == 2 && m_O_r1 == 1) { S_O++; }
                    else if (m_I_r1 == 2 && m_O_r1 == 2) { } //wrong
                }

                //check step 3
                if (check_flag1 && check_flag2)
                    if (N_I > S_I) { train[i].check = 0; }
                    else { train[i].check = 1; }
            }
            return train[i].check;
        }

        // fast check
        //注意几个方面：
        //1. 慢车与快车同时占用一个轨道的情况下，慢车必须先行
        //2. 注意相邻两列列车之间相继出发的headway问题
        //3. Debug图形调整运行图的问题        
        internal int fast_check_new(List<CTrain> train, int systime, int i)
        {
            //searchs for fast train after the focal train

            CParameter parameter = new CParameter();
            int i_curStationindex = train[i].route[train[i].CurRouteNode]; //
            int i_nextStationindex = train[i].route[train[i].CurRouteNode + 1]; //
            int i_runtime = train[i].SectionTime[train[i].route[train[i].CurRouteNode], train[i].NextNode]; //train i's section trip time in the next section

            //this train is a slow train
            int fast_k = 0; // 标记满足后车运行时间低于本车运行时间的列车个数           
            int dx_train_current = 0;//index of oppsite trains dwells at this station
            for (int k = 0; k < train.Count; k++)
                if (train[k].trainType != train[i].trainType)
                    if (train[k].Nodestatus < 3)
                        if (train[k].route[train[k].CurRouteNode] == train[i].route[train[i].CurRouteNode])
                            dx_train_current++;
            for (int k = 0; k < train.Count; k++)
            {
                //search for succeeding fast trains
                if (train[k].trainType == train[i].trainType && train[k].bdeparture == 1 && i != k && train[i].CurRouteNode != 0)
                {
                    int k_i_station_in_routek = -1;  // the route node of i's current station in k's route 
                    for (int s = 0; s < train[k].route.Count; s++) //search for the position of i's current station in train k's route, 寻找k的路径中i当前节点的序号 
                        if (train[k].route[s] == i_curStationindex)
                        { k_i_station_in_routek = s; break; }

                    if (train[k].CurRouteNode <= k_i_station_in_routek && train[k].route[k_i_station_in_routek + 1] == train[i].NextNode) // k will use i's current section  (start_node == start_node && end_node==end_node)
                    {
                        int k_runtime = 0;
                        for (int t = train[k].CurRouteNode; t < k_i_station_in_routek + 1; t++)
                        {
                            if (t == train[k].CurRouteNode)
                            {
                                if (train[k].Nodestatus == 1)
                                    k_runtime = k_runtime + train[k].dwellingtime[train[k].route[t]] - (systime - train[k].arrival[train[k].route[t]]) + train[k].SectionTime[train[k].route[t], train[k].route[t + 1]];
                                else if (train[k].Nodestatus == 2)
                                    k_runtime = k_runtime + train[k].SectionTime[train[k].route[t], train[k].route[t + 1]];
                                else if (train[k].Nodestatus == 3)
                                    k_runtime = k_runtime + train[k].SectionTime[train[k].route[t], train[k].route[t + 1]] - (systime - train[k].departure[train[k].route[t]]);
                            }
                            else
                                k_runtime = k_runtime + train[k].SectionTime[train[k].route[t], train[k].route[t + 1]] + train[k].dwellingtime[train[k].route[t]];
                        }

                        //if (k_runtime < DX_min_time)
                        if (k_runtime < i_runtime && (!(train[k].CurRouteNode == k_i_station_in_routek && train[k].Nodestatus == 3))) // train k is a fast train and travels on train i's last section
                        {
                            int k_check = check_capacity_LF(train, systime, k);
                            if (k_check == 1 && (dx_train_current <= 1 && train[i].CurRouteNode < train[i].route.Count - 1))
                            { fast_k++; }
                        }
                    }
                }
                if (fast_k > 0)
                {
                    train[i].check = 0;
                    break;
                }//train i's check turns out to be not pass
            }

            //判断下个节点当前存在的列车数目大于3个，本车不能继续向前运行
            int dx_train_nextnode = 0;//index of oppsite trains dwells at next station
            int bx_train_nextnode = 0;
            for (int k = 0; k < train.Count; k++)
                if (train[i].bdeparture == 1 && train[i].CurRouteNode < train[i].route.Count - 2)
                {
                    if (train[k].trainType != train[i].trainType)
                    {
                        if (train[k].Nodestatus < 3)
                            if (train[k].route[train[k].CurRouteNode] == train[i].route[train[i].CurRouteNode + 1])
                                dx_train_nextnode++;
                    }
                    else if (train[k].trainType == train[i].trainType)
                    {
                        if (train[k].Nodestatus < 3)
                            if (train[k].route[train[k].CurRouteNode] == train[i].route[train[i].CurRouteNode + 1])
                                bx_train_nextnode++;
                    }
                    if (dx_train_nextnode + bx_train_nextnode >= 3)
                    {
                        train[i].check = 0; break;
                    }
                }

            //下个区间上运行的有本向车，判断本车会否在下个区间追赶上上述同向列车
            int i_time = train[i].SectionTime[train[i].route[train[i].CurRouteNode], train[i].NextNode];
            for (int k = 0; k < train.Count; k++)
            {
                int k_time = 0;
                if (train[i].bdeparture == 1 && train[i].CurRouteNode < train[i].route.Count - 1)
                    if (train[k].trainType == train[i].trainType)
                        if (train[k].Nodestatus == 3)
                            if (train[k].route[train[k].CurRouteNode] == train[i].route[train[i].CurRouteNode])
                                k_time = train[k].SectionTime[train[k].route[train[k].CurRouteNode], train[k].NextNode] - (systime - train[k].departure[train[k].route[train[k].CurRouteNode]]) + train[k].dwellingtime[train[k].NextNode];
                if (i_time < k_time)
                {
                    train[i].check = 0; break;
                }
            }

            return train[i].check;
        }

        int headway_dytime(List<CTrain> train, int systime, int i)
        {
            CParameter parameter = new CParameter();
            int Dy_Time = parameter.Max_int;
            int min_k_time = parameter.Max_int;
            for (int k = 0; k < train.Count; k++)
            {
                if (train[k].trainType == train[i].trainType && k != i)
                    if (train[k].route[train[k].CurRouteNode] == train[i].route[train[i].CurRouteNode])
                    {
                        int current_node = train[i].route[train[i].CurRouteNode];
                        // k departs from the current station,train[k].departure[train[k].route[train[k].CurRouteNode]] > 0
                        if (train[k].Nodestatus == 3)
                        {
                            //find the nearest train
                            if (train[k].departure[train[k].route[train[k].CurRouteNode]] < min_k_time)
                            {
                                min_k_time = train[k].departure[train[k].route[train[k].CurRouteNode]];
                                train[i].headway_status = true; train[i].foretrainID = k;
                            }
                        }
                        else
                        {
                            //k and i use the same track, if i departs firstly, nothing to do 
                            if (train[i].track[train[i].route[train[i].CurRouteNode]] == train[k].track[train[k].route[train[k].CurRouteNode]])
                            {
                                if (train[i].arrival[current_node] < train[k].CurRouteNode)
                                {
                                    //train[i].headway_status = false;
                                }
                                else if (train[i].arrival[current_node] == train[k].CurRouteNode)
                                {
                                    if (i < k) // k is the succeding train 
                                    {
                                        // train[i].headway_status = false;
                                    }
                                    else
                                    { train[i].headway_status = true; train[i].foretrainID = k; }
                                }
                            }
                            else  // k and i use different tracks
                            {
                                if (train[i].speed < train[k].speed || (train[i].speed == train[k].speed && i > k))
                                {
                                    train[i].headway_status = true; train[i].foretrainID = k;
                                }
                            }
                        }
                    }
            }

            if (train[i].headway_status)
            {
                int f_time;
                //j.departuretime + headway
                if (train[train[i].foretrainID].departure[train[i].route[train[i].CurRouteNode]] > 0)
                    f_time = train[train[i].foretrainID].departure[train[i].route[train[i].CurRouteNode]] + parameter.Hdd;
                else
                    f_time = parameter.Max_int;
                //i.arrvaltime + i.dwell
                int b_time = train[i].arrival[train[i].route[train[i].CurRouteNode]] + train[i].dwellingtime[train[i].route[train[i].CurRouteNode]];

                int period = Math.Max(f_time, b_time) - systime;
                Dy_Time = Math.Max(0, period);
            }
            else
                Dy_Time = 0;
            return Dy_Time;
        }

        //determine discrete event
        public void Discrete_Definition(List<CTrain> train, int systime, int[] Dy_Time)
        {
            CParameter parameter = new CParameter();
            int Dy_decide_num = 0;
            bool[] Dy_decide = new bool[train.Count];
            for (int i = 0; i < train.Count; i++)
                Dy_decide[i] = false;
            int M = 100000; //a larger number

            for (int i = 0; i < train.Count; i++)
            {
                if (train[i].bdeparture == 0)
                {
                    if (train[i].arrival[train[i].route[0]] > systime) //尚未出发，且没达到出发条件                    
                        Dy_Time[i] = train[i].arrival[train[i].route[0]] - systime;
                    else if (train[i].arrival[train[i].route[0]] == systime)
                        Dy_Time[i] = 0;

                    Dy_decide[i] = true; Dy_decide_num++;
                }

                else if (train[i].bdeparture == 1)
                {
                    //dwell at the station
                    if (train[i].Nodestatus == 1)
                    {
                        Dy_Time[i] = train[i].dwellingtime[train[i].route[train[i].CurRouteNode]] - (systime - train[i].arrival[train[i].route[train[i].CurRouteNode]]);

                        Dy_decide[i] = true; Dy_decide_num++;
                    }

                    //prepare to leave the station
                    else if (train[i].Nodestatus == 2)
                    {
                        if (train[i].check == 1) //pass the check
                        {
                            if (train[i].NextNode == -1)
                                Dy_Time[i] = 0;
                            else
                                //Dy_Time[i] = 0;// train[i].SectionTime[train[i].route[train[i].CurRouteNode], train[i].NextNode];
                                Dy_Time[i] = headway_dytime(train, systime, i);

                            Dy_decide[i] = true; Dy_decide_num++;
                        }
                        else if (train[i].check == 0) //donot pass the check
                        {
                            Dy_Time[i] = M;
                            Dy_decide[i] = true; Dy_decide_num++;
                        }
                    }

                    //on the section
                    else if (train[i].Nodestatus == 3)
                    {
                        //section trip time - (systime - arrival time)
                        Dy_Time[i] = train[i].SectionTime[train[i].route[train[i].CurRouteNode], train[i].NextNode] - (systime - train[i].departure[train[i].route[train[i].CurRouteNode]]);
                        Dy_decide[i] = true; Dy_decide_num++;
                    }
                }
                else if (train[i].bdeparture == 2)
                {
                    Dy_Time[i] = M;
                    Dy_decide[i] = true; Dy_decide_num++;
                }
            }
            Debug.Assert(Dy_decide_num == train.Count);
        }

        //update system information
        public void Update_Information(List<CTrain> train, int systime, int[] Dy_Time)
        {
            CParameter parameter = new CParameter();
            int min_Dy_Time = Find_min_dytime(train, Dy_Time);
            int update_num = 0;
            bool[] update = new bool[train.Count];
            for (int i = 0; i < train.Count; i++)
                update[i] = false;

            for (int i = 0; i < train.Count; i++)
            {
                double train_travel_distance = 0;//当前事件中列车行走距离; outbound方向为正；inbound方向为负数              
                if (train[i].bdeparture == 0)
                {
                    if (Dy_Time[i] == min_Dy_Time) // lead to the system discrete event
                    {
                        train[i].arrival[train[i].route[0]] = systime + min_Dy_Time;
                        train[i].CurRouteNode = 0;
                        train[i].NextNode = train[i].route[train[i].CurRouteNode + 1];
                        train[i].time = systime + min_Dy_Time;
                        train[i].Nodestatus = 1;
                        train[i].bdeparture = 1;

                        //update position
                        train_travel_distance = 0;
                        update[i] = true; update_num++;
                    }
                    else
                    {
                        train[i].time = systime + min_Dy_Time;

                        //update position
                        train_travel_distance = 0;
                        update[i] = true; update_num++;
                    }
                }
                else if (train[i].bdeparture == 1)
                {
                    if (train[i].Nodestatus == 1)
                    {
                        if (Dy_Time[i] == min_Dy_Time) // lead to the system discrete event                       
                        { train[i].Nodestatus = 2; }

                        train[i].time = systime + min_Dy_Time;

                        //update position
                        train_travel_distance = 0;
                        update[i] = true; update_num++;
                    }
                    else if (train[i].Nodestatus == 2)
                    {
                        if (Dy_Time[i] == min_Dy_Time)
                        {
                            if (train[i].CurRouteNode == train[i].route.Count - 1)
                            { train[i].bdeparture = 2; }

                            train[i].headway_status = false; train[i].foretrainID = -1;

                            train[i].Nodestatus = 3;
                            train[i].departure[train[i].route[train[i].CurRouteNode]] = systime + min_Dy_Time;
                        }

                        train[i].time = systime + min_Dy_Time;

                        //update position
                        train_travel_distance = 0;
                        update[i] = true; update_num++;
                    }
                    else if (train[i].Nodestatus == 3)
                    {
                        if (Dy_Time[i] == min_Dy_Time)
                        {
                            train[i].Nodestatus = 1;
                            train[i].arrival[train[i].NextNode] = systime + min_Dy_Time;
                            train[i].CurRouteNode++;
                            if (train[i].CurRouteNode == train[i].route.Count - 1)
                                train[i].NextNode = -1;
                            else
                                train[i].NextNode = train[i].route[train[i].CurRouteNode + 1];

                            //加入 track[k][r]的分配规则。。。。。
                            int dx_train_current_station = 0, bx_train_current_station = 0; // 停靠在本站的对向/本向列车的数目计数    
                            List<int> dx_trainID = new List<int>();
                            List<int> bx_trainID = new List<int>();
                            bool[] track = new bool[parameter.C_Station];
                            for (int k = 0; k < parameter.C_Station; k++)
                            { track[k] = true; } //ture indexs that this track is free

                            for (int k = 0; k < train.Count; k++)
                            {
                                if (train[k].route[train[k].CurRouteNode] == train[i].route[train[i].CurRouteNode] && train[k].Nodestatus < 3 && train[k].track[train[k].route[train[k].CurRouteNode]] != -1)
                                {
                                    if (train[k].trainType == train[i].trainType)
                                    { bx_train_current_station++; bx_trainID.Add(k); }
                                    else
                                    { dx_train_current_station++; dx_trainID.Add(k); }
                                    track[train[k].track[train[k].route[train[k].CurRouteNode]]] = false;
                                }
                            }

                            int t_k = 0;
                            while (true)
                            {
                                // if their are two dx train and one bx train, then the focal train's track is the same to the fore train
                                if (t_k == parameter.C_Station)
                                {
                                    train[i].track[train[i].route[train[i].CurRouteNode]] = train[bx_trainID[0]].track[train[bx_trainID[0]].route[train[bx_trainID[0]].CurRouteNode]];
                                    train[i].foretrainID = bx_trainID[0];
                                    break;
                                }

                                if (track[t_k] == true) //this track is free which can be used by train i
                                {
                                    train[i].track[train[i].route[train[i].CurRouteNode]] = t_k;
                                    break;
                                }
                                t_k++;
                            }//while

                            if (bx_train_current_station > 0)
                                train[i].foretrainID = bx_trainID[0]; //the foretrainID 在node_status 1->2 的更新中删去
                        }

                        train[i].time = systime + min_Dy_Time;

                        //update position
                        train_travel_distance = train[i].speed * min_Dy_Time;
                        if (train[i].trainType == 1) train_travel_distance = 0 - train_travel_distance;
                        update[i] = true; update_num++;
                    }
                }
                else if (train[i].bdeparture == 2)
                {
                    train[i].time = systime + min_Dy_Time;

                    //update position
                    train_travel_distance = 0;
                    update[i] = true; update_num++;
                }

                //add position
                train[i].CurrentPosition = train[i].CurrentPosition + train_travel_distance;

                if (train[i].bdeparture == 0 || train[i].bdeparture == 2)
                    train[i].ListPosition.Add(-1);
                else
                    train[i].ListPosition.Add(train[i].CurrentPosition);

                train[i].ListTime.Add(systime + min_Dy_Time);
            }
            systime = systime + min_Dy_Time;
            Debug.Assert(update_num == train.Count);
        }

        internal int Find_min_dytime(List<CTrain> train, int[] Dy_Time)
        {
            int min_Dy_time = 100000000;
            for (int i = 0; i < train.Count; i++)
                min_Dy_time = min_Dy_time > Dy_Time[i] ? Dy_Time[i] : min_Dy_time;

            return min_Dy_time;
        }
    }

    public class COutput_ITAS_Result
    {
        public COutput_ITAS_Result(List<CTrain> train, List<CRailwayStation> station, List<CRailwaySection> section, int _nset)
        {
            Console.WriteLine();
            Console.WriteLine("******* ITAS IS COMPLETE **********");
            string defaultPath = System.IO.Directory.GetCurrentDirectory().ToString();//读取txt文件address            
            DirectoryInfo Timetable = new DirectoryInfo(@defaultPath + "\\Timetable"); //save this set of experiments in this file
            if (!Timetable.Exists)
                Timetable.Create();
            string inputdatapath = System.IO.Path.GetDirectoryName(@defaultPath + "\\Timetable" + "\\");
            StreamWriter ITAStimetable = File.CreateText(inputdatapath + "\\ITAS_Timetable" + _nset + ".txt");
            StreamWriter ITASstation = File.CreateText(inputdatapath + "\\ITASstation" + _nset + ".txt");

            ITASstation.Write("station1:0");
            for (int i = 0; i < section.Count; i++)
                ITASstation.Write(",station" + (i + 2) + ":" + section[i].length);
            ITASstation.WriteLine();
            ITASstation.WriteLine("横坐标：Time (s), 纵坐标：Position");
            ITASstation.Close();

            ITAStimetable.WriteLine("ITAS Scheduled Timetable ");
            for (int i = 0; i < train.Count; i++)
            {
                ITAStimetable.WriteLine("Train: " + (i + 1));
                for (int j = 0; j < station.Count; j++)
                    if (train[i].trainType == 0)
                        ITAStimetable.Write(train[i].arrival[j] + " " + train[i].departure[j] + " ");
                    else if (train[i].trainType == 1)
                        ITAStimetable.Write(train[i].departure[j] + " " + train[i].arrival[j] + " ");

                ITAStimetable.WriteLine();
            }
            ITAStimetable.Close();
        }
    }

    public class CInitialize_Information
    {
        public CInitialize_Information(List<CTrain> train, List<CRailwayStation> station, List<CRailwaySection> section)
        {
            CParameter parameter = new CParameter();
            int[,] sectionlength = new int[station.Count, station.Count];
            for (int i = 0; i < station.Count; i++)
                for (int j = 0; j < station.Count; j++)
                    sectionlength[i, j] = -1;
            double RailwayTotalLength = 0;
            for (int i = 0; i < section.Count; i++)
            {
                sectionlength[section[i].start_station_ID, section[i].end_station_ID] = section[i].length * 1000; //km to m
                sectionlength[section[i].end_station_ID, section[i].start_station_ID] = section[i].length * 1000; //km to m
                RailwayTotalLength = RailwayTotalLength + section[i].length * 1000;
            }

            int H = parameter.DepartureTimeInterval; // departure interval is 1 hour = 3600 s
            for (int i = 0; i < train.Count; i++)
            {
                train[i].SectionTime = new int[station.Count, station.Count];
                for (int j = 0; j < train[i].route.Count - 1; j++)
                    train[i].SectionTime[train[i].route[j], train[i].route[j + 1]] = (int)((sectionlength[train[i].route[j], train[i].route[j + 1]]) / train[i].speed);
                train[i].CurRouteNode = 0;
                train[i].NextNode = train[i].route[1];

                train[i].arrival = new int[train[i].route.Count];
                train[i].departure = new int[train[i].route.Count];

                //for double directions trains
                train[i].arrival[train[i].route[0]] = (int)(i / 2) * H;

                //for one direction trains
                //train[i].arrival[train[i].route[0]] = i * H;

                train[i].free_arrival = new int[train[i].route.Count];
                train[i].free_departure = new int[train[i].route.Count];
                for (int j = 0; j < train[i].route.Count; j++)
                {
                    if (j > 0)
                    { train[i].free_arrival[train[i].route[j]] = train[i].free_departure[train[i].route[j - 1]] + train[i].SectionTime[train[i].route[j - 1], train[i].route[j]]; }
                    else
                    { train[i].free_arrival[train[i].route[0]] = (int)(i / 2) * H; }
                    train[i].free_departure[train[i].route[j]] = train[i].free_arrival[train[i].route[j]] + train[i].dwellingtime[train[i].route[j]];
                }
                train[i].free_run_time = train[i].free_departure[train[i].route[train[i].route.Count - 1]] - train[i].free_arrival[train[i].route[0]];

                if (train[i].speed == 30) { train[i].cost_factor = 1 / train[i].free_run_time; }
                else { train[i].cost_factor = 1 / train[i].free_run_time; }

                train[i].track = new int[station.Count];
                train[i].headway_status = false;
                for (int k = 0; k < train[i].route.Count; k++)
                    train[i].track[train[i].route[k]] = -1;

                //20160722 记录列车在每个事件时的位置,以m为单位记录
                if (train[i].trainType == 0)
                {
                    train[i].ListPosition.Add(0);
                    train[i].CurrentPosition = 0;
                }
                else
                {
                    train[i].ListPosition.Add(RailwayTotalLength);
                    train[i].CurrentPosition = RailwayTotalLength;
                }
                train[i].ListTime.Add(0);
            }
        }
    }
}
