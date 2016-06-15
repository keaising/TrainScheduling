using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace TrainScheduling.Model
{
    [Serializable]
    public class Ctrain
    {
        public Ctrain()
        {
            this.trainID = new int();
            this.speed = new double();
            this.trainType = new int();
            this.route = new List<int>();
            this.check = new int();
            this.bdeparture = new int();
            this.dwellingtime = new List<int>();
            this.Nodestatus = new int();
            this.NextNode = new int();
            this.CurRouteNode = new int();
            this.time = new int();
            this.foretrainID = -1; //initialize -1
            this.free_run_time = new double();
            this.cost_factor = new double();
            this.Loco_prepare_using_ID = -1; //initialized as -1
            this.time_receive_loco = -1; //initialized as -1
            this.Loco_re_or_new = -1; //initialized as -1;
            this.Loco_using_ID = -1;
            //this.Clone();
        }
        public int trainID;
        public double speed;
        public int trainType; //0-outbound; 1-Inbound
        public List<int> route;
        public int check;     //capacity check 0-not pass 1-pass  
        public int bdeparture; //status of train departure; 0-no depart, 1-depart, 2-leave the system
        public int[] arrival; //arrival time
        public int[] departure;  //departure time
        public int[] free_arrival; //arrival time in free run case
        public int[] free_departure;  //departure time in free run case
        public double free_run_time;
        public double cost_factor;
        public List<int> dwellingtime; //dwelling time at each station
        public int Nodestatus;  //0-out the system, 1-just arrive a station(列车刚到站状态), 2-prepare to leave the staion(列车处于待出发状态)，3-on the section(列车在区间上)
        public int[,] SectionTime;
        public int NextNode;
        public int time;
        public int CurRouteNode; //train's current node's route index
        public int[] track;// the track of station K train i use [_ntrain,_nstation,station_capacity]; 
        public bool headway_status; // status of the train; =false dont need a headway ; =true need a headway
        public int foretrainID; // train's fore train which leads to a headway for the focal train; -1;

        public int Loco_using_ID; // the ID of the locomotive used by current train
        public int Loco_prepare_using_ID; // the ID of the locomotive the current train may use, it comes from the oppsoing train
        public int time_receive_loco; // the time that train receive the locomotive
        public int Loco_re_or_new; // illustrate whether the used locomotive is come from opposing (=1) or the new one (=0);

        //public Ctrain Clone()
        //{
        //    //    //return this as object;      //引用同一个对象
        //    //    return (Ctrain)this.MemberwiseClone(); //浅复制
        //    return new Ctrain() as Ctrain;//深复制         
        //}

        public Ctrain Clone()
        {
            using (Stream objectStream = new MemoryStream())
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(objectStream, this);
                objectStream.Seek(0, SeekOrigin.Begin);
                return formatter.Deserialize(objectStream) as Ctrain;
            }
        }
    }

    [Serializable]
    public class Crailway_station
    {
        public Crailway_station()
        {
            this.stationID = new int();
            this.stationCapacity = new int();
            this.front_station_ID = new List<int>();
            this.succeeding_station_ID = new List<int>();
            //this.Clone();
        }
        public int stationID;
        public int stationCapacity; //number of tracks at this station
        public List<int> front_station_ID;
        public List<int> succeeding_station_ID;
        //public Crailway_station Clone()
        //{
        //    //return this as object;      //引用同一个对象
        //    //return (Crailway_station)this.MemberwiseClone(); //浅复制
        //    return new Crailway_station() as Crailway_station;//深复制
        //}
        public Crailway_station Clone()
        {
            using (Stream objectStream = new MemoryStream())
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(objectStream, this);
                objectStream.Seek(0, SeekOrigin.Begin);
                return formatter.Deserialize(objectStream) as Crailway_station;
            }
        }
        //public Crailway_station Clone()
        //{
        //    Crailway_station other = (Crailway_station)this.MemberwiseClone();
        //    other.stationID = this.stationID;
        //    other.front_station_ID = this.front_station_ID;
        //    return other;
        //}
    }

    [Serializable]
    public class Crailway_section
    {
        public Crailway_section()
        {
            this.sectionID = new int();
            this.length = new int();
            this.start_station_ID = new int();
            this.end_station_ID = new int();
            //this.Clone();
        }
        public int sectionID;
        public int length;
        public int start_station_ID;
        public int end_station_ID;
        //public Crailway_section Clone()
        //{
        //    //return this as object;      //引用同一个对象
        //    //return (Crailway_section)this.MemberwiseClone(); //浅复制
        //    return new Crailway_section() as Crailway_section;//深复制
        //}
        public Crailway_section Clone()
        {
            using (Stream objectStream = new MemoryStream())
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(objectStream, this);
                objectStream.Seek(0, SeekOrigin.Begin);
                return formatter.Deserialize(objectStream) as Crailway_section;
            }
        }

    }

    public class Clocomotive
    {
        public Clocomotive()
        {
            this.locoID = new int();
            this.trainID_current_using_loco = new int();
            this.free = new bool();
            this.loco_speed = new double();
            this.trainID_all_using_loco = new List<int>();
        }
        public int locoID;
        public int trainID_current_using_loco; //this locomotive is used by train train_used_ID;
        public bool free; // whether this locomotive is used; free: true-not use/ false- used       
        public double loco_speed; //locomotive maximum speed.
        public List<int> trainID_all_using_loco; //record which train uses this loco
        public int Origin_route_ID; //current loco's location;  -1~ new for every train; x- train has been used and now it local at x; 
    }

    public class Copposing_train
    {
        public Copposing_train()
        {
            this.opposing_train_ID = new List<int>();
            this.opposing_train_time = new List<int>();
        }
        public List<int> opposing_train_ID;
        public List<int> opposing_train_time;
    }

    public class Crailway_system
    {
        public Crailway_system()
        {
            this.sys_train = new List<Ctrain>();
            this.sys_station = new List<Crailway_station>();
            this.sys_section = new List<Crailway_section>();
            this.sys_delay = new int();
        }
        public List<Ctrain> sys_train = new List<Ctrain>();
        public List<Crailway_section> sys_section = new List<Crailway_section>();
        public List<Crailway_station> sys_station = new List<Crailway_station>();
        public int sys_delay = new int(); //corresponding delay of current train timetable: total deviation of free-run time and obstacle-run time 

    }

    public class CDrawBase : System.Object, ICloneable
    {
        public string name = "jmj";
        public CDrawBase()
        {
        }

        public object Clone()
        {
            //return this as object;      //引用同一个对象
            return this.MemberwiseClone(); //浅复制
            //return new DrawBase() as object;//深复制
        }
    }

    public class COutput_Timetable_Result
    {
        public COutput_Timetable_Result(List<Ctrain> train, List<Crailway_station> station, List<Crailway_section> section, string method, int _nset)
        {
            Console.WriteLine();
            Console.WriteLine("******* " + method + " IS COMPLETE **********");
            string defaultPath = System.IO.Directory.GetCurrentDirectory().ToString();//读取txt文件address            
            DirectoryInfo Timetable = new DirectoryInfo(@defaultPath + "\\" + method + "_Timetable"); //save this set of experiments in this file
            if (!Timetable.Exists)
                Timetable.Create();
            string inputdatapath = System.IO.Path.GetDirectoryName(@defaultPath + "\\" + method + "_Timetable" + "\\");
            StreamWriter method_timetable = File.CreateText(inputdatapath + "\\" + method + "_Timetable_" + train.Count
                + "_trains_" + _nset + ".txt");
            StreamWriter method_station = File.CreateText(inputdatapath + "\\" + method + "_Station_" + _nset + ".txt");

            method_station.Write("station1:0");
            for (int i = 0; i < section.Count; i++)
                method_station.Write(",station" + (i + 2) + ":" + section[i].length);
            method_station.WriteLine();
            method_station.WriteLine("横坐标：Time (s), 纵坐标：Position");
            method_station.Close();

            method_timetable.WriteLine(method + " Scheduled Timetable ");
            for (int i = 0; i < train.Count; i++)
            {
                method_timetable.WriteLine("Train: " + (i + 1));
                for (int j = 0; j < station.Count; j++)
                    if (train[i].trainType == 0)
                        method_timetable.Write(train[i].arrival[j] + " " + train[i].departure[j] + " ");
                    else if (train[i].trainType == 1)
                        method_timetable.Write(train[i].departure[j] + " " + train[i].arrival[j] + " ");

                method_timetable.WriteLine();
            }
            method_timetable.Close();
        }
    }

}


