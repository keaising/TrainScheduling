using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrainScheduling.Model
{
    class CParameter
    {
        public int Haa = 3 * 60;
        public int Hdd = 3 * 60;
        public int Had = 3 * 60;
        public int Hda = 3 * 60;
        public int C_Station = 10;
        public int Max_int = 10000000;
        public int total_time_limit = 3; //* 60 * 60;//60 * 60 * 1;// unit is sec
        public int node_time_limit = 15 * 60; // unit is sec

        //Dtrain parameter
        public int min_nbtrain = 8;
        public int max_nbtrain = 26;
        public int Dtrain_data_input_nbstation = 0; //station case in test_train case

        //Dstation parameter
        public int _nset_start = 0; // minimum nb of stations in  test_station case
        public int _nTestSet = 20;  // maximum nb of stations in  test_station case
        public int Dstation_data_input_nbtrain = 26; //number of trains in test_station case

        //loco_assginment_algorithm
        public int time_threshold_loco = 55 * 60; //the time_threshold for train to use the locomotive from oppposing train
        public int time_shunting = 8 * 60; //shunting time for locomotive at terminal station

        public int fast_speed = 50;
        public int slow_speed = 30;
        public double Time_co = 0.5;
        public double Loco_co = 0.5;
    }
}
