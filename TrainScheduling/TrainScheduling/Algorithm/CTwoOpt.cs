﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrainScheduling.Model;

/*****************************************************************
 * 算法思想：经过2-opt给出不同的列车辅画顺序，做列车timetable，取最好的。
 * 重点在于 train timetbale generation method
 * Idea: with the given train scheduling sequence generated by 2-opt algorithm, 
 * then output train timetable. search for the best train timetable. 
*******************************************************************/

namespace TrainScheduling.Algorithm
{
    public class CTwoOpt
    {
        public CTwoOpt(List<Ctrain> train, List<Crailway_station> station, List<Crailway_section> section, StreamWriter TwoOpt_output, int _nset)
        {
            Crailway_system railway_sys = new Crailway_system();
            foreach (var obj in train)
                railway_sys.sys_train.Add(obj);
            foreach (var obj in station)
                railway_sys.sys_station.Add(obj);
            foreach (var obj in section)
                railway_sys.sys_section.Add(obj);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            railway_sys = CTwoOpt_main(railway_sys, TwoOpt_output, _nset);
            sw.Stop();
            int TwoOpt_cpu_Time = Int32.Parse(sw.ElapsedMilliseconds.ToString()) / 1000; // unit is sec           

            TwoOpt_output.WriteLine(_nset + "\t" + railway_sys.sys_train.Count + "\t" +
                railway_sys.sys_delay + "\t" + TwoOpt_cpu_Time);

            //output_timetable
            COutput_Timetable_Result TwoOpt_result = new COutput_Timetable_Result(railway_sys.sys_train, railway_sys.sys_station,
                railway_sys.sys_section, "TwoOpt", _nset);

            train.Clear();
            foreach (var obj in railway_sys.sys_train)
                train.Add(obj);
        }

        public Crailway_system CTwoOpt_main(Crailway_system _railway_system, StreamWriter TwoOpt_output, int _nset)
        {
            CParameter parameter = new CParameter();
            Crailway_system rand_best_railway_system = new Crailway_system();
            List<int> train_priority_seq = new List<int>();
            //initial priority sequence and system information
            foreach (var obj in _railway_system.sys_train)
            {
                train_priority_seq.Add(obj.trainID);
                rand_best_railway_system.sys_train.Add(obj);
            }

            //Randomly generate several initial sequence equal to train number. 
            List<CSequence> Sequence_set = new List<CSequence>();
            int rand_count = train_priority_seq.Count(); int rand_count_id = 0;
            while (rand_count >= 0)
            {
                CSequence set_seq = new CSequence();
                set_seq.inital_seq = random_sequence(train_priority_seq);
                set_seq.seq_ID = rand_count_id;
                Sequence_set.Add(set_seq);
                rand_count_id++;
                rand_count--;
            }

            //construct set of rand_railway_system to store generated railway_system for each random sequence
            List<Crailway_system> Rand_railway_system = new List<Crailway_system>();

            //repeat until no improvement is made
            int improve = 0;

            string defaultPath = System.IO.Directory.GetCurrentDirectory().ToString();//读取txt文件address           
            DirectoryInfo Dstation_Experiment = new DirectoryInfo(@defaultPath + "\\Dstations_TwoOpt"); //save this set of experiments in this file
            if (!Dstation_Experiment.Exists)
                Dstation_Experiment.Create();

            StreamWriter StatisticResultText = File.CreateText(Dstation_Experiment + "\\TwoOpt_log_" + train_priority_seq.Count + "_trains_" + _nset + "_nset.txt");
            StatisticResultText.WriteLine("TwoOpt start: ");
            //if swap 20 times, there is no improvement, stop
            while (improve < Sequence_set.Count())
            {
                StatisticResultText.WriteLine("**************-----current seq_ID is  "
                    + improve + " -------*******************");

                Crailway_system best_railway_system = new Crailway_system();
                foreach (var obj in _railway_system.sys_section)
                    best_railway_system.sys_section.Add(obj.Clone());
                foreach (var obj in _railway_system.sys_station)
                    best_railway_system.sys_station.Add(obj.Clone());
                best_railway_system.sys_delay = parameter.Max_int;
                List<int> rand_train_priority_seq = new List<int>();
                //initial priority sequence and system information
                foreach (var obj in _railway_system.sys_train)
                    best_railway_system.sys_train.Add(obj.Clone());

                foreach (var obj in Sequence_set[improve].inital_seq)
                    rand_train_priority_seq.Add(obj);

                //get train size
                int size = rand_train_priority_seq.Count();

                int best_delay = best_railway_system.sys_delay;
                List<int> best_train_priority_seq = new List<int>();
                for (int i = 0; i < size; i++)
                    best_train_priority_seq.Add(rand_train_priority_seq[i]);

                for (int i = 0; i < size - 1; i++)
                {
                    for (int k = i + 1; k < size; k++)
                    {
                        List<int> new_train_priority_seq = new List<int>();
                        new_train_priority_seq = TwoOptSwap(best_train_priority_seq, i, k);
                        Crailway_system new_railway_system = new Crailway_system();

                        //每次调用初始值，而不会改变初始化的 railway_system 里面的值
                        Crailway_system test_railway_system = new Crailway_system();
                        foreach (var obj in _railway_system.sys_train)
                            test_railway_system.sys_train.Add(obj.Clone());
                        foreach (var obj in _railway_system.sys_station)
                            test_railway_system.sys_station.Add(obj.Clone());
                        foreach (var obj in _railway_system.sys_section)
                            test_railway_system.sys_section.Add(obj.Clone());
                        //**timetable generation function**//
                        new_railway_system = Greedy_Timetable_generation(test_railway_system, new_train_priority_seq);
                        //**timetable generation function**//

                        int new_delay = new_railway_system.sys_delay;
                        if (new_delay < best_delay)
                        {
                            //Improvement found, then reset
                            //improve = 0;
                            rand_train_priority_seq.Clear();
                            foreach (var obj in new_train_priority_seq)
                                rand_train_priority_seq.Add(obj);
                            best_delay = new_delay;

                            // clear current items
                            best_railway_system.sys_train.Clear();

                            //update current best train_timetable
                            foreach (var obj in new_railway_system.sys_train)
                                best_railway_system.sys_train.Add(obj);
                            best_railway_system.sys_delay = best_delay;
                            best_train_priority_seq.Clear();
                            foreach (var obj in new_train_priority_seq)
                                best_train_priority_seq.Add(obj);
                        }

                        //StatisticResultText.WriteLine("new sequence: ");
                        for (int t = 0; t < new_train_priority_seq.Count(); t++)
                            StatisticResultText.Write(new_train_priority_seq[t] + " ");
                        StatisticResultText.WriteLine(" | corresponding delay is:  " + new_delay);
                    }

                }

                Rand_railway_system.Add(best_railway_system);
                foreach (var obj in best_train_priority_seq)
                {
                    int obj_value = obj;
                    Sequence_set[improve].best_seq.Add(obj_value);
                }
                Sequence_set[improve].seq_delay = best_delay;

                improve++;

                StatisticResultText.WriteLine("best_train_priority_seq is: ");
                for (int t = 0; t < best_train_priority_seq.Count(); t++)
                    StatisticResultText.Write(best_train_priority_seq[t] + " ");
                StatisticResultText.WriteLine(" | corresponding best delay is: " + best_delay);
            }
            int min_delay = Rand_railway_system.Min(x => x.sys_delay);
            rand_best_railway_system = Rand_railway_system.Find(x => x.sys_delay == min_delay);

            StatisticResultText.WriteLine("With given random suquence, best_train_priority_seq is: ");
            for (int t = 0; t < rand_best_railway_system.sys_train.Count(); t++)
                StatisticResultText.Write(Sequence_set.Find(x => x.seq_delay == min_delay).best_seq[t] + " ");

            StatisticResultText.WriteLine(" | corresponding best delay is: " + min_delay + " Initial seq_ID is: "
                + Sequence_set.Find(x => x.seq_delay == min_delay).seq_ID);

            StatisticResultText.Close();
            return rand_best_railway_system;
        }

        public List<int> TwoOptSwap(List<int> _seq, int _i, int _k)
        {
            int i = _i, k = _k;
            List<int> new_train_priority_seq = new List<int>();

            //1. take seq[0] to seq[i-1] and add them in oder to new_seq
            for (int c = 0; c <= i - 1; ++c)
                new_train_priority_seq.Add(_seq[c]);

            //2. take seq[i] to seq[k] and add them in reverse order to new_seq
            int dec = 0;
            for (int c = i; c <= k; ++c)
            {
                new_train_priority_seq.Add(_seq[k - dec]);
                dec++;
            }

            //3. take seq[k+1] to end and add them in order to new_seq
            for (int c = k + 1; c <= _seq.Count() - 1; ++c)
                new_train_priority_seq.Add(_seq[c]);

            return new_train_priority_seq;
        }

        public Crailway_system Greedy_Timetable_generation(Crailway_system _railway_system, List<int> _train_priority_seq)
        {
            CParameter parameter = new CParameter();
            Crailway_system railway_system = new Crailway_system();
            foreach (var obj in _railway_system.sys_train)
                railway_system.sys_train.Add(obj);
            foreach (var obj in _railway_system.sys_station)
                railway_system.sys_station.Add(obj);
            foreach (var obj in _railway_system.sys_section)
                railway_system.sys_section.Add(obj);

            for (int seq_i = 0; seq_i < railway_system.sys_train.Count(); seq_i++)
            {
                int i = _train_priority_seq[seq_i];
                for (int k = 0; k < railway_system.sys_train[i].route.Count() - 1; k++)
                {
                    //store train i's departure time     
                    List<int> dep_list = new List<int>();
                    List<int> arr_list = new List<int>();

                    //pre-planned departure time for train i from k: d_i_k
                    int pre_d_i_k = new int(); int pre_a_i_kk = new int();

                    //determine the pre-planed trains, free_run_time
                    int sub_size = _train_priority_seq.FindIndex(x => x.Equals(i));

                    #region
                    ////对向所有比本车优先的列车的最晚到达本站时间+hda
                    //int Reverse_t_arr_hda = 0;
                    //for (int j_c = 0; j_c < sub_size; j_c++)
                    //{
                    //    int j = _train_priority_seq[j_c];
                    //    if (railway_system.sys_train[i].trainType != railway_system.sys_train[j].trainType)
                    //        Reverse_t_arr_hda = Math.Max(Reverse_t_arr_hda,
                    //            railway_system.sys_train[j].departure[railway_system.sys_train[i].route[k]] + parameter.Hda);
                    //}
                    #endregion

                    //if train i at the origin station, 
                    if (k == 0)
                    {
                        pre_d_i_k = railway_system.sys_train[i].free_departure[railway_system.sys_train[i].route[k]];
                        pre_a_i_kk = railway_system.sys_train[i].free_departure[railway_system.sys_train[i].route[k]] +
                            railway_system.sys_train[i].SectionTime[railway_system.sys_train[i].route[k], railway_system.sys_train[i].route[k + 1]];
                    }
                    else
                    {
                        pre_d_i_k = railway_system.sys_train[i].arrival[railway_system.sys_train[i].route[k]] +
                            railway_system.sys_train[i].dwellingtime[railway_system.sys_train[i].route[k]];
                        pre_a_i_kk = pre_d_i_k + railway_system.sys_train[i].SectionTime[railway_system.sys_train[i].route[k], railway_system.sys_train[i].route[k + 1]];
                    }

                    //train i is the head train
                    if (sub_size == 0)
                    {
                        int[] d_a_k_kk = new int[2];
                        d_a_k_kk = computation_1_1(railway_system.sys_train[i], k, pre_d_i_k);
                        dep_list.Add(d_a_k_kk[0]);
                        arr_list.Add(d_a_k_kk[1]);
                    }
                    //others
                    else
                    {
                        bool pre_update = false; bool has_cross = true;
                        while (has_cross)
                        {
                            has_cross = false;
                            Chas_cross Has_Cross = new Chas_cross();
                            //before update
                            if (!pre_update)
                            {
                                //pre_d_i_k=pre_d_i_k
                                //function
                                Has_Cross = determin_pre(railway_system, pre_d_i_k, pre_a_i_kk, _train_priority_seq, i, k);
                                for (int t = 0; t < Has_Cross.arr_List.Count(); t++)
                                {
                                    dep_list.Add(Has_Cross.dep_List[t]); arr_list.Add(Has_Cross.arr_List[t]);
                                }
                                has_cross = Has_Cross.has_Cross;
                                pre_update = true;
                            }
                            else
                            {
                                //update pre_d_i_k 
                                pre_d_i_k = dep_list.Max(); pre_a_i_kk = arr_list.Max();
                                dep_list.Clear(); arr_list.Clear();
                                //function 
                                Has_Cross = determin_pre(railway_system, pre_d_i_k, pre_a_i_kk, _train_priority_seq, i, k);
                                for (int t = 0; t < Has_Cross.arr_List.Count(); t++)
                                {
                                    dep_list.Add(Has_Cross.dep_List[t]); arr_list.Add(Has_Cross.arr_List[t]);
                                }
                                has_cross = Has_Cross.has_Cross;
                                pre_update = true;
                            }
                        }

                        #region
                        //for (int j_c = 0; j_c < sub_size; j_c++)
                        //{
                        //    int j = _train_priority_seq[j_c];
                        //    int[] d_a_k_kk = new int[2];
                        //    //travelling in the same direction
                        //    if (railway_system.sys_train[i].trainType == railway_system.sys_train[j].trainType)
                        //    {
                        //        //situation 1.1
                        //        if (pre_d_i_k < railway_system.sys_train[j].departure[railway_system.sys_train[i].route[k]] - parameter.Hdd
                        //            && pre_a_i_kk < railway_system.sys_train[j].arrival[railway_system.sys_train[i].route[k + 1]] - parameter.Haa)
                        //            d_a_k_kk = computation_1_1(railway_system.sys_train[i], k);
                        //        //situation 1.2
                        //        else if (pre_d_i_k > railway_system.sys_train[j].departure[railway_system.sys_train[i].route[k]] + parameter.Hdd
                        //            && pre_a_i_kk > railway_system.sys_train[j].arrival[railway_system.sys_train[i].route[k + 1]] + parameter.Haa)
                        //            d_a_k_kk = computation_1_1(railway_system.sys_train[i], k);
                        //        //situation 1.3
                        //        else
                        //            d_a_k_kk = computation_1_2(railway_system.sys_train[i], railway_system.sys_train[j], k);
                        //    }
                        //    //travelling in different directions
                        //    else
                        //    {
                        //        //situation 2.1
                        //        if (pre_d_i_k < railway_system.sys_train[j].arrival[railway_system.sys_train[i].route[k]] - parameter.Hda
                        //           && pre_a_i_kk < railway_system.sys_train[j].departure[railway_system.sys_train[i].route[k + 1]] - parameter.Hda)
                        //            d_a_k_kk = computation_1_1(railway_system.sys_train[i], k);
                        //        //situation 2.2
                        //        else if (pre_d_i_k > railway_system.sys_train[j].arrival[railway_system.sys_train[i].route[k]] + parameter.Had
                        //            && pre_a_i_kk > railway_system.sys_train[j].departure[railway_system.sys_train[i].route[k + 1]] + parameter.Hda)
                        //            d_a_k_kk = computation_1_1(railway_system.sys_train[i], k);
                        //        //situation 2.3
                        //        else
                        //            d_a_k_kk = computation_2_2(railway_system.sys_train[i], railway_system.sys_train[j], k);
                        //    }
                        //    dep_list.Add(d_a_k_kk[0]);
                        //    arr_list.Add(d_a_k_kk[1]);
                        //}
                        #endregion
                    }
                    //update train_i's departure time at station k and arrival time at station k+
                    railway_system.sys_train[i].departure[railway_system.sys_train[i].route[k]] = dep_list.Max();
                    railway_system.sys_train[i].arrival[railway_system.sys_train[i].route[k + 1]] = arr_list.Max();
                }

                //update train i's departure time at terminal station
                railway_system.sys_train[i].departure[railway_system.sys_train[i].route[railway_system.sys_train[i].route.Count() - 1]]
                    = railway_system.sys_train[i].arrival[railway_system.sys_train[i].route[railway_system.sys_train[i].route.Count() - 1]];
            }
            //calculate total delay of trains 
            int delay = 0;
            for (int i = 0; i < railway_system.sys_train.Count(); i++)
                delay = delay + (railway_system.sys_train[i].departure[railway_system.sys_train[i].route[railway_system.sys_train[i].route.Count() - 1]]
                    - railway_system.sys_train[i].free_departure[railway_system.sys_train[i].route[railway_system.sys_train[i].route.Count() - 1]]);

            railway_system.sys_delay = delay;
            return railway_system;
        }

        public Crailway_system Cplex_based_Timetable_generation(Crailway_system _railway_system, List<int> _train_priority_seq)
        {
            CParameter parameter = new CParameter();
            Crailway_system railway_system = new Crailway_system();

            return railway_system;
        }

        //train_j's timetable is fixed
        private int[] computation_1_1(Ctrain train_i, int station_k, int pre_d_i_k)
        {
            CParameter parameter = new CParameter();
            int[] d_a_k_kk = new int[2];
            int dep_time = new int(), arr_time = new int();
            dep_time = Math.Max(train_i.arrival[train_i.route[station_k]] + train_i.dwellingtime[train_i.route[station_k]], pre_d_i_k);
            arr_time = dep_time + train_i.SectionTime[train_i.route[station_k], train_i.route[station_k + 1]];
            d_a_k_kk[0] = dep_time; d_a_k_kk[1] = arr_time;
            return d_a_k_kk;
        }

        private int[] computation_1_2(Ctrain train_i, Ctrain train_j, int station_k, int pre_d_i_k)
        {
            CParameter parameter = new CParameter();
            int[] d_a_k_kk = new int[2];
            int dep_time = new int(), arr_time = new int();
            //d_i_k = max{d_j_k+haa, head_arr_kk - section_time, pre_planed_dep}
            int head_arr_kk = train_j.arrival[train_i.route[station_k + 1]] + parameter.Haa;
            int head_arr_kk_dep = head_arr_kk - train_i.SectionTime[train_i.route[station_k], train_i.route[station_k + 1]];
            int head_dep = train_j.departure[train_i.route[station_k]] + parameter.Hdd;
            int pre_dep = train_i.arrival[train_i.route[station_k]] + train_i.dwellingtime[train_i.route[station_k]];
            //determine departure time from k and arrival time at k+
            dep_time = Math.Max(head_dep, pre_dep);
            dep_time = Math.Max(head_arr_kk_dep, dep_time);
            dep_time = Math.Max(pre_d_i_k, dep_time);
            arr_time = dep_time + train_i.SectionTime[train_i.route[station_k], train_i.route[station_k + 1]];
            d_a_k_kk[0] = dep_time; d_a_k_kk[1] = arr_time;
            return d_a_k_kk;
        }

        //similar to compitation 1_1 so call computaion 1_1
        private int[] computation_2_1(Ctrain train_i, Ctrain train_j, int station_k)
        {
            CParameter parameter = new CParameter();
            int[] d_a_k_kk = new int[2];
            int dep_time = parameter.Max_int, arr_time = parameter.Max_int;
            d_a_k_kk[0] = dep_time; d_a_k_kk[1] = arr_time;
            return d_a_k_kk;
        }

        private int[] computation_2_2(Ctrain train_i, Ctrain train_j, int station_k, int pre_d_i_k)
        {
            CParameter parameter = new CParameter();
            int[] d_a_k_kk = new int[2];
            int dep_time = new int(), arr_time = new int();
            int head_arr_kk = train_j.departure[train_i.route[station_k + 1]] + parameter.Hda;
            int head_arr_kk_dep = head_arr_kk - train_i.SectionTime[train_i.route[station_k], train_i.route[station_k + 1]];
            int head_dep = train_j.arrival[train_i.route[station_k]] + parameter.Had;// +train_i.SectionTime[train_i.route[station_k], train_i.route[station_k + 1]];
            int pre_dep = train_i.arrival[train_i.route[station_k]] + train_i.dwellingtime[train_i.route[station_k]];
            dep_time = Math.Max(head_dep, pre_dep);
            dep_time = Math.Max(head_arr_kk_dep, dep_time);
            dep_time = Math.Max(pre_d_i_k, dep_time);
            arr_time = dep_time + train_i.SectionTime[train_i.route[station_k], train_i.route[station_k + 1]];
            d_a_k_kk[0] = dep_time; d_a_k_kk[1] = arr_time;
            return d_a_k_kk;
        }

        private Chas_cross determin_pre(Crailway_system railway_system, int pre_d_i_k, int pre_a_i_kk, List<int> _train_priority_seq, int train_i, int station_k)
        {
            Chas_cross Has_Cross = new Chas_cross();
            int i = train_i, k = station_k; bool has_cross = false;
            int sub_size = _train_priority_seq.FindIndex(x => x.Equals(i));
            CParameter parameter = new CParameter();
            #region
            for (int j_c = 0; j_c < sub_size; j_c++)
            {
                int j = _train_priority_seq[j_c];
                int[] d_a_k_kk = new int[2];
                //travelling in the same direction
                if (railway_system.sys_train[i].trainType == railway_system.sys_train[j].trainType)
                {
                    //situation 1.1
                    if (pre_d_i_k <= railway_system.sys_train[j].departure[railway_system.sys_train[i].route[k]] - parameter.Hdd
                        && pre_a_i_kk <= railway_system.sys_train[j].arrival[railway_system.sys_train[i].route[k + 1]] - parameter.Haa)
                        d_a_k_kk = computation_1_1(railway_system.sys_train[i], k, pre_d_i_k);
                    //situation 1.2
                    else if (pre_d_i_k >= railway_system.sys_train[j].departure[railway_system.sys_train[i].route[k]] + parameter.Hdd
                        && pre_a_i_kk >= railway_system.sys_train[j].arrival[railway_system.sys_train[i].route[k + 1]] + parameter.Haa)
                        d_a_k_kk = computation_1_1(railway_system.sys_train[i], k, pre_d_i_k);
                    //situation 1.3 there is a cross between tains i and j
                    else
                    {
                        d_a_k_kk = computation_1_2(railway_system.sys_train[i], railway_system.sys_train[j], k, pre_d_i_k);
                        has_cross = true;
                    }
                }
                //travelling in different directions
                else
                {
                    //situation 2.1
                    if (pre_d_i_k <= railway_system.sys_train[j].arrival[railway_system.sys_train[i].route[k]] - parameter.Hda
                       && pre_a_i_kk <= railway_system.sys_train[j].departure[railway_system.sys_train[i].route[k + 1]] - parameter.Hda)
                        d_a_k_kk = computation_1_1(railway_system.sys_train[i], k, pre_d_i_k);
                    //situation 2.2
                    else if (pre_d_i_k >= railway_system.sys_train[j].arrival[railway_system.sys_train[i].route[k]] + parameter.Had
                        && pre_a_i_kk >= railway_system.sys_train[j].departure[railway_system.sys_train[i].route[k + 1]] + parameter.Hda)
                        d_a_k_kk = computation_1_1(railway_system.sys_train[i], k, pre_d_i_k);
                    //situation 2.3 there is a cross between trains i and j
                    else
                    {
                        d_a_k_kk = computation_2_2(railway_system.sys_train[i], railway_system.sys_train[j], k, pre_d_i_k);
                        has_cross = true;
                    }
                }
                Has_Cross.dep_List.Add(d_a_k_kk[0]);
                Has_Cross.arr_List.Add(d_a_k_kk[1]);
                Has_Cross.has_Cross = has_cross;
            }
            return Has_Cross;
            #endregion
        }

        public class Chas_cross
        {
            public Chas_cross()
            {
                this.arr_List = new List<int>();
                this.dep_List = new List<int>();
                this.has_Cross = new bool();
            }
            public List<int> dep_List;
            public List<int> arr_List;
            public bool has_Cross;
        }

        public class CSequence
        {
            public CSequence()
            {
                this.seq_ID = new int();
                this.inital_seq = new List<int>();
                this.seq_delay = new int();
                this.best_seq = new List<int>();
            }
            public List<int> inital_seq;
            public List<int> best_seq;
            public int seq_delay;
            public int seq_ID;
        }

        private List<int> random_sequence(List<int> inital_sequence)
        {
            List<int> rand_seq = new List<int>();
            int[] seq = new int[inital_sequence.Count];
            for (int i = 0; i < seq.Count(); i++)
            {
                seq[i] = inital_sequence[i]; rand_seq.Add(seq[i]);
            }

            for (int i = 0; i < inital_sequence.Count(); i++)
            {
                int rand_position = new int();
                Random rand = new Random(Guid.NewGuid().GetHashCode());
                rand_position = rand.Next(0, inital_sequence.Count());
                int store = rand_seq[i];
                rand_seq[i] = rand_seq[rand_position];
                rand_seq[rand_position] = store;
            }
            return rand_seq;
        }
        #region
        /*
         * 选择排序法就是按照顺序从余下数中选出最小（大）的数，和顺序位置的数字交换，反复进行。
         * 此法最多可能会交换n-1次，比如[4,1,2,3]递增排序中的4就需要挪3次，当然最少一次也不用。
         * 但是随机算法循环次数无法浮动，必须是固定的，怎么办呢？没有关系，我们可以引入废操作，
         * 位置已经摆对的数自己和自己交换，这样就可以让所有顺序排序都成为n-1步走。
         * 反过来想就明白了，从0开始每个位置和后面的随机位置交换，也可以和自己交换，
         * 直到n-2和n-1（或n-2自己交换），就可以得到一个随机数组。
         */
        #endregion
    }
}
