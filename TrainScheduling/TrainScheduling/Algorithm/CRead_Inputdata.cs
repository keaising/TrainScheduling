﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using TrainScheduling.Model;

namespace TrainScheduling.Algorithm
{
    public class CRead_Inputdata
    {
        public CRead_Inputdata()
        { }
        public void CRead_Inputdata_return(StreamReader LB_input_train_data, StreamReader LB_input_station_data, StreamReader LB_input_section_data, List<Ctrain> train, List<Crailway_station> station, List<Crailway_section> section)
        {
            //Console.WriteLine("******Start to read input data******");
            input_train_data(LB_input_train_data, train);
            Console.WriteLine();
            Console.WriteLine("****** Input TRAIN Data is Read Successfully ******");
            input_network_data(LB_input_station_data, station);
            Console.WriteLine();
            Console.WriteLine("****** Input STATION Data is Read Successfully ******");
            input_section_data(LB_input_section_data, section);
            Console.WriteLine();
            Console.WriteLine("****** Input SECTION Data is Read Successfully ******");
        }

        //read train data
        public void input_train_data(StreamReader LB_input_train_data, List<Ctrain> train)
        {
            string Input_data; int ROW = 0; string ss = "SPLIT";
            while ((Input_data = LB_input_train_data.ReadLine()) != null)
            {
                int flag = 0; string[] arrstr = Input_data.Split('\t');
                if (ROW > 1)
                {
                    Ctrain thetrain = new Ctrain();
                    int hasCount = arrstr.Where(delegate (string s) { return !string.IsNullOrEmpty(s); }).Count();
                    int split = 0;
                    while (flag < hasCount)
                    {
                        if (flag == 0)
                            thetrain.trainID = int.Parse(arrstr[flag]);
                        else if (flag == 1)
                            thetrain.speed = double.Parse(arrstr[flag]);
                        else if (flag == 2)
                            thetrain.trainType = int.Parse(arrstr[flag]);
                        else
                        {
                            if (arrstr[flag] == ss)
                                split = 1;
                            else
                            {
                                if (split == 0)
                                    thetrain.route.Add(int.Parse(arrstr[flag]));
                                else if (split == 1)
                                    thetrain.dwellingtime.Add(int.Parse(arrstr[flag]));
                            }
                        }
                        flag++;
                    }
                    train.Add(thetrain);
                }
                ROW++;
            }
        }

        //read station data
        public void input_network_data(StreamReader LB_input_station_data, List<Crailway_station> station)
        {
            string Input_data = null; int ROW = 0; string ss = "SPLIT";
            while ((Input_data = LB_input_station_data.ReadLine()) != null)
            {
                int flag = 0; string[] arrstr = Input_data.Split('\t');
                int split = 0;
                if (ROW > 1)
                {
                    Crailway_station thestation = new Crailway_station();
                    int hasCount = arrstr.Where(delegate (string s) { return !string.IsNullOrEmpty(s); }).Count();
                    while (flag < hasCount)
                    {
                        if (flag == 0)
                            thestation.stationID = int.Parse(arrstr[flag]);
                        else if (flag == 1)
                            thestation.stationCapacity = int.Parse(arrstr[flag]);
                        else if (flag > 1)
                        {
                            if (arrstr[flag] == ss)
                                split = 1;
                            else
                            {
                                if (split == 0)
                                    thestation.front_station_ID.Add(int.Parse(arrstr[flag]));
                                else if (split == 1)
                                    thestation.succeeding_station_ID.Add(int.Parse(arrstr[flag]));
                            }
                        }
                        flag++;
                    }
                    station.Add(thestation);
                }
                ROW++;
            }
        }

        //read section data 
        public void input_section_data(StreamReader LB_input_section_data, List<Crailway_section> section)
        {
            string Input_data; int ROW = 0;
            while ((Input_data = LB_input_section_data.ReadLine()) != null)
            {
                if (ROW > 1)
                {
                    Crailway_section thesection = new Crailway_section();
                    int flag = 0; string[] arrstr = Input_data.Split('\t');
                    int hasCount = arrstr.Where(delegate (string s) { return !string.IsNullOrEmpty(s); }).Count();
                    while (flag < hasCount)
                    {
                        if (flag == 0)
                            thesection.sectionID = int.Parse(arrstr[flag]);
                        else if (flag == 1)
                            thesection.length = int.Parse(arrstr[flag]);
                        else if (flag == 2)
                            thesection.start_station_ID = int.Parse(arrstr[flag]);
                        else
                            thesection.end_station_ID = int.Parse(arrstr[flag]);
                        flag++;
                    }
                    section.Add(thesection);
                }
                ROW++;
            }
        }

        public void input_loco_data(StreamReader LB_input_loco_data, List<Clocomotive> loco)
        {
            string Input_data; int ROW = 0;
            while ((Input_data = LB_input_loco_data.ReadLine()) != null)
            {
                if (ROW > 1)
                {
                    Clocomotive theloco = new Clocomotive();
                    int flag = 0; string[] arrstr = Input_data.Split('\t');
                    int hasCount = arrstr.Where(delegate (string s) { return !string.IsNullOrEmpty(s); }).Count();
                    while (flag < hasCount)
                    {
                        if (flag == 0)
                            theloco.locoID = int.Parse(arrstr[flag]);
                        else if (flag == 1)
                            theloco.loco_speed = int.Parse(arrstr[flag]);
                        flag++;
                    }
                    loco.Add(theloco);
                }
                ROW++;
            }
        }
    }
}