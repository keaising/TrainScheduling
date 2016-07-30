using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;


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
        public int DepartureTimeInterval = 1 * 60 * 60;//列车发车间隔

        //Dtrain parameter
        public int min_nbtrain = 8;
        public int max_nbtrain = 26;
        public int Dtrain_data_input_nbstation = 0; //station case in test_train case

        //Dstation parameter
        public int _nset_start = 0; // minimum nb of stations in  test_station case
        public int _nTestSet = 20;  // maximum nb of stations in  test_station case
        public int Dstation_data_input_nbtrain = 30; //number of trains in test_station case

        //loco_assginment_algorithm
        public int time_threshold_loco = 55 * 60; //the time_threshold for train to use the locomotive from oppposing train
        public int time_shunting = 8 * 60; //shunting time for locomotive at terminal station

        public int fast_speed = 50;
        public int slow_speed = 30;
        public double Time_co = 0.5;
        public double Loco_co = 0.5;

        //转化HexCode到rgb颜色 
        //调用方式为 Brush br = new SolidBrush(HexColor("#3ea530"));
        public Color HexColor(String hex)
        {
            //將井字號移除
            hex = hex.Replace("#", "");

            byte a = 255;
            byte r = 255;
            byte g = 255;
            byte b = 255;
            int start = 0;

            //處理ARGB字串 
            if (hex.Length == 8)
            {
                a = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                start = 2;
            }

            // 將RGB文字轉成byte
            r = byte.Parse(hex.Substring(start, 2), System.Globalization.NumberStyles.HexNumber);
            g = byte.Parse(hex.Substring(start + 2, 2), System.Globalization.NumberStyles.HexNumber);
            b = byte.Parse(hex.Substring(start + 4, 2), System.Globalization.NumberStyles.HexNumber);

            return Color.FromArgb(a, r, g, b);
        }



        public string[] HexCode = new string[] {"Blue","BlueViolet","DarkBlue","DeepPink","Indigo","Magenta","MidnightBlue","Purple","DarkOrchid"};

        //public string[] HexCode = new string[] {"Transparent","AliceBlue","AntiqueWhite","Aqua","Aquamarine","Azure","Beige","Bisque","Black","BlanchedAlmond",
        //    "Blue","BlueViolet","Brown","BurlyWood","CadetBlue","Chartreuse","Chocolate","Coral","CornflowerBlue","Cornsilk","Crimson","Cyan","DarkBlue","DarkCyan",
        //    "DarkGoldenrod","DarkGray","DarkGreen","DarkKhaki","DarkMagenta","DarkOliveGreen","DarkOrange","DarkOrchid","DarkRed","DarkSalmon",
        //    "DarkSeaGreen","DarkSlateBlue","DarkSlateGray","DarkTurquoise","DarkViolet","DeepPink","DeepSkyBlue","DimGray","DodgerBlue","Firebrick",
        //    "FloralWhite","ForestGreen","Fuchsia","Gainsboro","GhostWhite","Gold","Goldenrod","Gray","Green","GreenYellow","Honeydew","HotPink",
        //    "IndianRed","Indigo","Ivory","Khaki","Lavender","LavenderBlush","LawnGreen","LemonChiffon","LightBlue","LightCoral","LightCyan","LightGoldenrodYellow",
        //    "LightGreen","LightGray","LightPink","LightSalmon","LightSeaGreen","LightSkyBlue","LightSlateGray","LightSteelBlue","LightYellow","Lime",
        //    "LimeGreen","Linen","Magenta","Maroon","MediumAquamarine","MediumBlue","MediumOrchid","MediumPurple","MediumSeaGreen","MediumSlateBlue",
        //    "MediumSpringGreen","MediumTurquoise","MediumVioletRed","MidnightBlue","MintCream","MistyRose","Moccasin","NavajoWhite","Navy","OldLace",
        //    "Olive","OliveDrab","Orange","OrangeRed","Orchid","PaleGoldenrod","PaleGreen","PaleTurquoise","PaleVioletRed","PapayaWhip","PeachPuff",
        //    "Peru","Pink","Plum","PowderBlue","Purple","Red","RosyBrown","RoyalBlue","SaddleBrown","Salmon","SandyBrown","SeaGreen","SeaShell",
        //    "Sienna","Silver","SkyBlue","SlateBlue","SlateGray","Snow","SpringGreen","SteelBlue","Tan","Teal","Thistle","Tomato","Turquoise","Violet",
        //    "Wheat","White","WhiteSmoke","Yellow","YellowGreen"};
    }
}
