using ILOG.Concert;
using ILOG.CPLEX;
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
    class CUSING_CPLEX
    {
        public CUSING_CPLEX(List<CTrain> train, List<CRailwayStation> station, List<CRailwaySection> section,
            StreamWriter CPLEX_output, int _nset)
        {

            //*****pure cplex to solve the problem
            Console.WriteLine();
            Console.WriteLine("------------  Run CPLEX_pure: _nbtrain = " + train.Count + ", _nset = " + _nset + "  -------------");
            Console.WriteLine();
            Cplex_Pure(train, station, section, CPLEX_output, _nset);

            #region
            /*
            CParameter parameter = new CParameter();
            int _ntrain = train.Count, _nstation = station.Count;
            int total_time_limit = parameter.total_time_limit, node_time_limit = parameter.node_time_limit; // unit is sec
            //int LB_k = 10; // the value of k in LB;
            //int elapsed_time = 0;

            double[][] a, d; double[][][] xiaa, xidd, xiad, xida, myu; double[][][][] omega;
            a = new double[_ntrain][]; d = new double[_ntrain][]; xiaa = new double[_ntrain][][]; xidd = new double[_ntrain][][];
            xiad = new double[_ntrain][][]; xida = new double[_ntrain][][]; myu = new double[_ntrain][][]; omega = new double[_ntrain][][][];
            for (int i = 0; i < _ntrain; i++)
            {
                a[i] = new double[_nstation]; d[i] = new double[_nstation]; xiaa[i] = new double[_ntrain][]; xidd[i] = new double[_ntrain][];
                xiad[i] = new double[_ntrain][]; xida[i] = new double[_ntrain][]; myu[i] = new double[_nstation][]; omega[i] = new double[_ntrain][][];
                for (int j = 0; j < _ntrain; j++)
                {
                    xiaa[i][j] = new double[_nstation]; xidd[i][j] = new double[_nstation]; xiad[i][j] = new double[_nstation]; xida[i][j] = new double[_nstation];
                    omega[i][j] = new double[_nstation][];
                    for (int k = 0; k < _nstation; k++)
                    { omega[i][j][k] = new double[_nstation]; }
                }
                for (int k = 0; k < _nstation; k++)
                { myu[i][k] = new double[parameter.C_Station]; }
            }

            //import initial solution
            Initial_solution(train, station, a, d, xiaa, xidd, xiad, xida, myu, omega);
            double ITAS_UB = 0;
            for (int i = 0; i < _ntrain; i++)
                ITAS_UB = ITAS_UB + train[i].departure[train[i].route[train[i].route.Count - 1]];
            */
            #endregion
        }

        internal void Initial_solution(List<CTrain> train, List<CRailwayStation> station,
            double[][] a, double[][] d, double[][][] xiaa, double[][][] xidd, double[][][] xiad, double[][][] xida, double[][][] myu, double[][][][] omega)
        {
            CParameter Parameter = new CParameter();
            int _ntrain = train.Count, _nstation = station.Count;

            //a, d
            for (int i = 0; i < _ntrain; i++)
                for (int k = 0; k < _nstation; k++)
                { a[i][k] = train[i].arrival[k]; d[i][k] = train[i].departure[k]; }

            // xiaa, xidd, xiad, xida
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    for (int k = 0; k < _nstation; k++)
                    {
                        if (train[i].trainType == train[j].trainType)
                        {
                            if (train[i].arrival[k] <= train[j].arrival[k])
                                xiaa[i][j][k] = 1;
                            else
                                xiaa[i][j][k] = 0;

                            if (train[i].departure[k] <= train[j].departure[k])
                                xidd[i][j][k] = 1;
                            else
                                xidd[i][j][k] = 0;
                        }
                        else
                        {
                            if (train[i].arrival[k] <= train[j].departure[k])
                                xiad[i][j][k] = 1;
                            else
                                xiad[i][j][k] = 0;

                            if (train[i].departure[k] <= train[j].arrival[k])
                                xida[i][j][k] = 1;
                            else
                                xida[i][j][k] = 0;
                        }
                    }

            //myu
            for (int i = 0; i < _ntrain; i++)
                for (int k = 0; k < _nstation; k++)
                    if (0 < k && k < _nstation - 1)
                    {
                        for (int c = 0; c < Parameter.C_Station; c++)
                        {
                            if (train[i].track[k] == c)
                                myu[i][k][c] = 1;
                            else
                                myu[i][k][c] = 0;
                        }
                    }
                    else
                    { myu[i][k][0] = 1; myu[i][k][1] = 0; myu[i][k][2] = 0; }

            //omega
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    if (train[i].trainType != train[j].trainType)
                        for (int k = 0; k < train[i].route.Count - 1; k++)
                        {
                            if (train[i].departure[train[i].route[k]] + Parameter.Hda <= train[j].arrival[train[i].route[k]]
                                && train[i].arrival[train[i].route[k + 1]] + Parameter.Had <= train[j].departure[train[i].route[k + 1]])
                            { omega[i][j][train[i].route[k]][train[i].route[k + 1]] = 1; }
                            else
                                omega[i][j][train[i].route[k]][train[i].route[k + 1]] = 0;
                        }
        }

        //main function of Local Branching
        internal void LB_main(List<CTrain> train, List<CRailwayStation> station, int LB_k, int total_time_limit, int node_time_limit,
            int elapsed_time, double[][] a, double[][] d, double[][][] xiaa, double[][][] xidd, double[][][] xiad, double[][][] xida, double[][][] myu,
            double[][][][] omega, double UB, StreamWriter LB_output, int _nset)
        {
            Cplex model = new Cplex();
            CParameter Parameter = new CParameter(); int Max_int = Parameter.Max_int;
            //initialization             
            int _ntrain = train.Count, _nstation = station.Count;
            INumVar[][] A = new INumVar[_ntrain][];//, _nstation]; //arrival time
            INumVar[][] D = new INumVar[_ntrain][];//, _nstation]; //departure time
            INumVar[][][] xiAA = new INumVar[_ntrain][][];//, _ntrain, _nstation];
            INumVar[][][] xiDD = new INumVar[_ntrain][][];//, _ntrain, _nstation];
            INumVar[][][] xiAD = new INumVar[_ntrain][][];//, _ntrain, _nstation];
            INumVar[][][] xiDA = new INumVar[_ntrain][][];//, _ntrain, _nstation];            
            INumVar[][][] Myu = new INumVar[_ntrain][][]; //_nstation, station_capacity
            INumVar[][][][] Omega = new INumVar[_ntrain][][][];//, _ntrain, _nstation, _nstation];
            NumVarType[] train_IntType = new NumVarType[_ntrain];
            for (int i = 0; i < _ntrain; i++)
            { train_IntType[i] = NumVarType.Int; }

            NumVarType[] station_IntType = new NumVarType[_nstation];
            for (int k = 0; k < _nstation; k++)
            { station_IntType[k] = NumVarType.Int; }

            NumVarType[] C_IntType = new NumVarType[Parameter.C_Station];
            for (int c = 0; c < Parameter.C_Station; c++)
            { C_IntType[c] = NumVarType.Int; }

            double[] lb_train = new double[_ntrain], up_train = new double[_ntrain];
            for (int i = 0; i < _ntrain; i++) { lb_train[i] = 0.0; up_train[i] = System.Double.MaxValue; }

            double[] lb_station = new double[_nstation], up_station = new double[_nstation];
            for (int k = 0; k < _nstation; k++) { lb_station[k] = 0.0; up_station[k] = System.Double.MaxValue; }

            double[] lb_c = new double[Parameter.C_Station], up_c = new double[Parameter.C_Station];
            for (int c = 0; c < Parameter.C_Station; c++) { lb_c[c] = 0.0; up_c[c] = 1.0; }

            //A,D                 
            for (int i = 0; i < _ntrain; i++)
            {
                A[i] = model.NumVarArray(_nstation, lb_station, up_station, station_IntType);
                D[i] = model.NumVarArray(_nstation, lb_station, up_station, station_IntType);
            }

            //xiaa, xidd, xiad, xida
            for (int i = 0; i < _ntrain; i++)
            {
                xiAA[i] = new INumVar[_ntrain][];
                xiDD[i] = new INumVar[_ntrain][];
                xiAD[i] = new INumVar[_ntrain][];
                xiDA[i] = new INumVar[_ntrain][];
                for (int j = 0; j < _ntrain; j++)
                {
                    xiAA[i][j] = model.NumVarArray(_nstation, lb_station, up_station, station_IntType);
                    xiDD[i][j] = model.NumVarArray(_nstation, lb_station, up_station, station_IntType);
                    xiAD[i][j] = model.NumVarArray(_nstation, lb_station, up_station, station_IntType);
                    xiDA[i][j] = model.NumVarArray(_nstation, lb_station, up_station, station_IntType);

                    for (int k = 0; k < _nstation; k++)
                    {
                        xiAA[i][j][k] = model.IntVar(0, 1);
                        xiDD[i][j][k] = model.IntVar(0, 1);
                        xiAD[i][j][k] = model.IntVar(0, 1);
                        xiDA[i][j][k] = model.IntVar(0, 1);
                    }
                }
            }

            //myu
            for (int i = 0; i < _ntrain; i++)
            {
                Myu[i] = new INumVar[_nstation][];
                for (int k = 0; k < _nstation; k++)
                {
                    Myu[i][k] = model.NumVarArray(Parameter.C_Station, lb_c, up_c, C_IntType);
                    for (int c = 0; c < Parameter.C_Station; c++)
                        Myu[i][k][c] = model.IntVar(0, 1);
                }
            }

            //omega
            for (int i = 0; i < _ntrain; i++)
            {
                Omega[i] = new INumVar[_ntrain][][];
                for (int j = 0; j < _ntrain; j++)
                {
                    Omega[i][j] = new INumVar[_nstation][];
                    for (int k = 0; k < _nstation; k++)
                    {
                        Omega[i][j][k] = model.NumVarArray(_nstation, lb_station, up_station, station_IntType);
                        for (int n = 0; n < _nstation; n++)
                        { Omega[i][j][k][n] = model.IntVar(0, 1); }
                    }
                }
            }

            INumExpr objective = model.IntExpr();
            for (int i = 0; i < _ntrain; i++)
                objective = model.Sum(D[i][train[i].route[train[i].route.Count - 1]], objective);

            IObjective obj_function = model.Minimize(objective);
            IObjective no_function = model.Minimize();

            model.Add(obj_function);
            //model.Add(no_function);

            // import_constraints
            Import_constraints(model, A, D, xiAA, xiDD, xiAD, xiDA, Myu, Omega, train, station);

            //import innitial solution
            model.Use(new Solve(A, D, xiAA, xiDD, xiAD, xiDA, Myu, Omega, a, d, xiaa, xidd, xiad, xida, myu, omega));

            //COutput_CPLEX_Solution cplex2 = new COutput_CPLEX_Solution(a, d, 0);

            string defaultPath = System.IO.Directory.GetCurrentDirectory().ToString();//读取txt文件address
            DirectoryInfo Experiment = new DirectoryInfo(@defaultPath + "\\LB_Statistic_Result_" + train.Count + "_trains"); //save this set of experiments in this file
            if (!Experiment.Exists)
                Experiment.Create();
            string LB_inputdatapath = System.IO.Path.GetDirectoryName(@defaultPath + "\\LB_Statistic_Result_" + train.Count + "_trains" + "\\");
            StreamWriter LB_record = File.CreateText(LB_inputdatapath + "\\LB_record_" + train.Count + "_trains_" + _nset + "_case.txt");
            StreamWriter LB_simple_record = File.CreateText(LB_inputdatapath + "\\LB_simple_record_" + train.Count + "_trains_" + _nset + "_case.txt");

            DirectoryInfo LB_Log_Experiment = new DirectoryInfo(@defaultPath + "\\LB_Statistic_Result"); //save this set of experiments in this file
            if (!LB_Log_Experiment.Exists)
                LB_Log_Experiment.Create();
            string LB_outputdatapath = System.IO.Path.GetDirectoryName(@defaultPath + "\\LB_Statistic_Result" + "\\");

            //double rhs = System.Double.MaxValue, 
            double TL = System.Double.MaxValue;
            int rhs = LB_k; //System.Int16.MaxValue;;
            double ITAS_obj = UB; UB = System.Double.MaxValue;
            double BestUB = UB;
            //bool opt = true;
            bool diversify = false; int dv = 0; int DV = 30; int no_improved = 0; int opt_no_improve = 5; int No_improved = 5;
            //bool reverse = true;
            bool first = true;

            double[][] inter_a, inter_d; double[][][] inter_xiaa, inter_xidd, inter_xiad, inter_xida, inter_myu; double[][][][] inter_omega;
            inter_a = new double[_ntrain][]; inter_d = new double[_ntrain][]; inter_xiaa = new double[_ntrain][][]; inter_xidd = new double[_ntrain][][];
            inter_xiad = new double[_ntrain][][]; inter_xida = new double[_ntrain][][]; inter_myu = new double[_ntrain][][]; inter_omega = new double[_ntrain][][][];
            for (int i = 0; i < _ntrain; i++)
            {
                inter_a[i] = new double[_nstation]; inter_d[i] = new double[_nstation]; inter_xiaa[i] = new double[_ntrain][]; inter_xidd[i] = new double[_ntrain][];
                inter_xiad[i] = new double[_ntrain][]; inter_xida[i] = new double[_ntrain][]; inter_myu[i] = new double[_nstation][]; inter_omega[i] = new double[_ntrain][][];
                for (int j = 0; j < _ntrain; j++)
                {
                    inter_xiaa[i][j] = new double[_nstation]; inter_xidd[i][j] = new double[_nstation]; inter_xiad[i][j] = new double[_nstation];
                    inter_xida[i][j] = new double[_nstation]; inter_omega[i][j] = new double[_nstation][];
                    for (int k = 0; k < _nstation; k++)
                    { inter_omega[i][j][k] = new double[_nstation]; }
                }
                for (int k = 0; k < _nstation; k++)
                { inter_myu[i][k] = new double[Parameter.C_Station]; }
            }

            int depth_node = 1;
            //double UBound = 0, LBound = 0;
            LB_record.WriteLine("Record the the information of each depath_node");
            LB_record.WriteLine("------------------------------------------------------------------------------------------------");
            LB_simple_record.WriteLine("simple record of solution solved by LB");
            LB_simple_record.WriteLine("Time \t solution");

            TextWriter LB_log = File.CreateText(@LB_outputdatapath + "\\LB_log_" + train.Count + "_trains_" + _nset + "_case.txt");
            //model.SetOut(LB_log);
            while (elapsed_time <= total_time_limit && dv <= DV && no_improved < No_improved)
            {
                LB_record.WriteLine("Current depth node is:\t" + depth_node);
                LB_record.WriteLine("Current rhs is:\t" + rhs);
                //begin to record the cpu time
                Stopwatch sw = new Stopwatch();
                sw.Start();


                ///import innitial solution
                //model.Use(new Solve(A, D, xiAA, xiDD, xiAD, xiDA, Myu, Omega, a, d, xiaa, xidd, xiad, xida, myu, omega));
                // INumExpr lbk for local braching constraint              
                IRange last_constraint = Import_last_LEconstraint(model, xiAA, xiDD, xiAD, xiDA, Myu, Omega, xiaa, xidd, xiad, xida, myu, omega, rhs);

                if (rhs < 1000)
                {
                    model.Use(new Solve(A, D, xiAA, xiDD, xiAD, xiDA, Myu, Omega, a, d, xiaa, xidd, xiad, xida, myu, omega));
                    model.Add(last_constraint);
                }

                //model.Add(last_constraint);

                // model.ExportModel("LB_org_model.Lp");

                //set compute time of this node, TL
                model.SetParam(Cplex.DoubleParam.TiLim, Math.Min(node_time_limit, total_time_limit - elapsed_time));
                //set UB        
                model.SetParam(Cplex.DoubleParam.CutUp, UB);
                //set tolarence gap
                model.SetParam(Cplex.DoubleParam.EpGap, 0.00);

                //if (first) { model.Delete(obj_function); model.Add(no_function); reverse = false; first = false; }
                //else if (!reverse && !first) { model.Delete(no_function); model.Add(obj_function); reverse = true; }              

                try
                {
                    if (model.Solve())
                    {
                        LB_record.WriteLine(" ************ Root Node Solution status = " + model.GetStatus() + " **********");

                        if (model.GetStatus().Equals(Cplex.Status.Optimal))
                        {
                            //has improved solution
                            if (model.GetObjValue() < BestUB - 0.1)
                            {
                                System.Console.WriteLine("\tImproved Solution (Optimal) is Found\t");
                                LB_record.WriteLine("\tImproved Solution (Optimal) is Found\t");
                                BestUB = model.GetObjValue();

                                UB = BestUB;
                                Update_reference_solution(model, A, D, xiAA, xiDD, xiAD, xiDA, Myu, Omega, inter_a, inter_d, inter_xiaa, inter_xidd, inter_xiad, inter_xida, inter_myu, inter_omega);

                                //reverse the last local braching constraint
                                model.Delete(last_constraint);
                                IRange right_constraint = Import_last_GEconstraint(model, xiAA, xiDD, xiAD, xiDA, Myu, Omega, xiaa, xidd, xiad, xida, myu, omega, rhs + 1);
                                model.Add(right_constraint);
                                Pass_new_solution(a, d, xiaa, xidd, xiad, xida, myu, omega, inter_a, inter_d, inter_xiaa, inter_xidd, inter_xiad, inter_xida, inter_myu, inter_omega);
                                rhs = LB_k; no_improved = 0;
                            }

                            else  //has no improved solution
                            {
                                System.Console.WriteLine("\tImproved Solution (optimal) is NOT Found\t");
                                LB_record.WriteLine("\tImproved Solution (optimal) is NOT Found\t");

                                //reverse the last local braching constraint
                                model.Delete(last_constraint);
                                IRange right_constraint = Import_last_GEconstraint(model, xiAA, xiDD, xiAD, xiDA, Myu, Omega, xiaa, xidd, xiad, xida, myu, omega, rhs + 1);
                                model.Add(right_constraint); rhs = rhs + LB_k / 2; opt_no_improve++; no_improved++;
                            }

                            diversify = false; first = false;
                            LB_record.WriteLine(" ************ BestUB is:\t" + BestUB + " **********");
                            LB_record.WriteLine(" ************ UB is:\t" + UB + " **********");
                        }

                        if (model.GetStatus().Equals(Cplex.Status.Feasible))
                        {
                            if (model.GetObjValue() < BestUB)
                            {
                                System.Console.WriteLine("\tImproved Solution (Feasible) is Found\t");
                                LB_record.WriteLine("\tImproved  (Feasible) Solution is Found\t");
                                BestUB = model.GetObjValue();
                            }
                            Update_reference_solution(model, A, D, xiAA, xiDD, xiAD, xiDA, Myu, Omega, inter_a, inter_d, inter_xiaa, inter_xidd, inter_xiad, inter_xida, inter_myu, inter_omega);
                            UB = model.GetObjValue();

                            if (first)
                            { model.Delete(last_constraint); }
                            else
                            {
                                model.Delete(last_constraint);
                                IRange right_constraint = Import_last_GEconstraint(model, xiAA, xiDD, xiAD, xiDA, Myu, Omega, xiaa, xidd, xiad, xida, myu, omega, 1);
                                model.Add(right_constraint);
                            }
                            Pass_new_solution(a, d, xiaa, xidd, xiad, xida, myu, omega, inter_a, inter_d, inter_xiaa, inter_xidd, inter_xiad, inter_xida, inter_myu, inter_omega);
                            first = false; diversify = false; rhs = LB_k;
                            //if (model.GetObjValue() < BestUB - 0.1)
                            //{
                            //    System.Console.WriteLine("\tImproved Solution (Feasible) is Found\t");
                            //    LB_record.WriteLine("\tImproved  (Feasible) Solution is Found\t");
                            //    BestUB = model.GetObjValue();

                            //    UB = model.GetObjValue();
                            //    Update_reference_solution(model, A, D, xiAA, xiDD, xiAD, xiDA, Myu, Omega, inter_a, inter_d, inter_xiaa, inter_xidd, inter_xiad, inter_xida, inter_myu, inter_omega);

                            //    model.Delete(last_constraint); rhs = LB_k; no_improved = 0; first = false; diversify = false;

                            //    Pass_new_solution(a, d, xiaa, xidd, xiad, xida, myu, omega, inter_a, inter_d, inter_xiaa, inter_xidd, inter_xiad, inter_xida, inter_myu, inter_omega);
                            //}
                            //else
                            //{
                            //    System.Console.WriteLine("\tImproved Solution (Feasible) is NOT Found\t");
                            //    LB_record.WriteLine("\tImproved Solution (Feasible) is NOT Found\t");

                            //    //this is the first time no improved solution, reduce explore area
                            //    if (!diversify)
                            //    {
                            //        UB = model.GetObjValue(); model.Delete(last_constraint); rhs = rhs - (int)LB_k / 2;
                            //        //no_improved = true; 
                            //        first = false;
                            //    }
                            //    //this is the second time no improved solution, reverse constraint  //strong deversity, abort this hand
                            //    else
                            //    {
                            //        model.Delete(last_constraint); UB = System.Double.MaxValue; first = true;
                            //        IRange right_constraint = Import_last_GEconstraint(model, xiAA, xiDD, xiAD, xiDA, Myu, Omega, xiaa, xidd, xiad, xida, myu, omega, 1);
                            //        model.Add(right_constraint);
                            //        rhs = (rhs > LB_k ? rhs : LB_k) + (int)LB_k / 2; //no_improved ++;
                            //    }
                            //    diversify = true;
                            //}
                            LB_record.WriteLine(" ************ BestUB is:\t" + BestUB + " **********");
                            LB_record.WriteLine(" ************ UB is:\t" + UB + " **********");
                        }

                        if (model.GetStatus().Equals(Cplex.Status.Infeasible))
                        {
                            System.Console.WriteLine("\tiIt has been Infeasible\t");
                            LB_record.WriteLine("\tIt has been proved Infeasible\t");
                            //abort this hand, >= k+1
                            model.Delete(last_constraint);
                            IRange right_constraint = Import_last_GEconstraint(model, xiAA, xiDD, xiAD, xiDA, Myu, Omega, xiaa, xidd, xiad, xida, myu, omega, LB_k + 1);
                            model.Add(right_constraint);

                            if (diversify) { UB = System.Double.MaxValue; dv++; first = true; }
                            rhs = (int)LB_k + LB_k / 2; diversify = true;
                        }
                    }
                    else
                    {
                        LB_record.WriteLine(" ************ This problem can not be solved (or node cpu time is over) **********");
                        System.Console.WriteLine("This problem can not be solved (or node cpu time is over)");
                        model.Delete(last_constraint);
                        if (diversify)
                        {
                            IRange right_constraint = Import_last_GEconstraint(model, xiAA, xiDD, xiAD, xiDA, Myu, Omega, xiaa, xidd, xiad, xida, myu, omega, 1);
                            model.Add(right_constraint);
                            UB = System.Double.MaxValue;
                            rhs = (int)(rhs + Math.Ceiling((double)LB_k / 2)); dv++; first = true;
                        }
                        else
                            rhs = (int)(rhs - Math.Ceiling((double)LB_k / 2));

                        diversify = true;
                    }
                    depth_node++;
                }
                catch (ILOG.Concert.Exception e)
                {
                    System.Console.WriteLine("Concert exception caught: " + e);
                    System.Console.Write(" Press any key to exit ...");
                    Console.ReadKey(true);
                    System.Environment.Exit(0);  // end this project
                }
                //model.End();

                sw.Stop();
                int node_cpu_Time;
                node_cpu_Time = Int32.Parse(sw.ElapsedMilliseconds.ToString()) / 1000; // unit is sec
                elapsed_time += node_cpu_Time;
                LB_record.WriteLine("Node_compute time is:\t" + node_cpu_Time + "\t (s);\t" + "Elapsed time is:\t" + elapsed_time + "\t(s)");
                LB_record.WriteLine("------------------------------------------------------------------------------------------------");
                LB_simple_record.WriteLine(elapsed_time + "\t" + BestUB);
            }
            LB_record.WriteLine("Total elapsed time is:\t" + elapsed_time + "\t (s);\t dv_count is:\t" + dv);
            LB_record.WriteLine("LB_obj is:\t" + BestUB + "\t(s);\tITAS_obj is:\t" + ITAS_obj + "\t(s);\tOptimal gap is:\t" + (ITAS_obj - BestUB) / BestUB);
            LB_record.Close();

            LB_output.WriteLine(_nset + "\t" + train.Count + "\t" + BestUB + "\t" + elapsed_time);

            TL = total_time_limit - elapsed_time;
            double tl = Math.Max(1, TL);
            model.SetParam(Cplex.DoubleParam.TiLim, tl);
            //model.Use(new Solve(A, D, xiAA, xiDD, xiAD, xiDA, Myu, Omega, a, d, xiaa, xidd, xiad, xida, myu, omega));
            //model.SetParam(Cplex.DoubleParam.CutUp, UB);
            //model.Solve();
        }


        internal void Pass_new_solution(double[][] a, double[][] d, double[][][] xiaa, double[][][] xidd, double[][][] xiad, double[][][] xida, double[][][] myu, double[][][][] omega,
            double[][] inter_a, double[][] inter_d, double[][][] inter_xiaa, double[][][] inter_xidd, double[][][] inter_xiad, double[][][] inter_xida, double[][][] inter_myu, double[][][][] inter_omega)
        {
            int _ntrain = a.GetLength(0), _nstation = a[0].GetLength(0);

            //A,D
            for (int i = 0; i < a.GetLength(0); i++)
            { a[i] = inter_a[i]; d[i] = inter_d[i]; }

            //xiAA, _xiDD, _xiAD,  _xiDA
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    if (i != j)
                    {
                        xiaa[i][j] = inter_xiaa[i][j]; xidd[i][j] = inter_xidd[i][j];
                        xiad[i][j] = inter_xiad[i][j]; xida[i][j] = inter_xida[i][j];
                    }

            //Myu
            for (int i = 0; i < myu.GetLength(0); i++)
                for (int j = 0; j < myu.GetLength(0); j++)
                    if (i != j)
                        for (int k = 1; k < myu[i].GetLength(0) - 1; k++)
                        { myu[i][k] = inter_myu[i][k]; }

            //_Omega
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    if ((i % 2 == 0 && j % 2 == 1) || (j % 2 == 0 && i % 2 == 1))
                        for (int k = 0; k < _nstation - 1; k++)
                        { omega[i][j][k] = inter_omega[i][j][k]; }
        }

        //import constraint; NOTE Thers is NO obj_function
        internal void Import_constraints(Cplex model, INumVar[][] A, INumVar[][] D, INumVar[][][] xiAA, INumVar[][][] xiDD, INumVar[][][] xiAD,
       INumVar[][][] xiDA, INumVar[][][] Myu, INumVar[][][][] Omega, List<CTrain> train, List<CRailwayStation> station)
        {
            CParameter Parameter = new CParameter();
            int Max_int = Parameter.Max_int;
            int _ntrain = train.Count; int _nstation = station.Count;

            //a i,k + t i,k ≤ d i,k ∀i ∈ N; k ∈ R i 
            for (int i = 0; i < _ntrain; i++)
                for (int k = 0; k < train[i].route.Count; k++)
                    model.AddLe(model.Sum(train[i].dwellingtime[train[i].route[k]], A[i][train[i].route[k]]), D[i][train[i].route[k]]);

            //d i,k + t_{i}^{k,k^+} ≤ a i,k^+  ∀i ∈ N; k ∈ R i \Des(i) 
            for (int i = 0; i < _ntrain; i++)
                for (int k = 0; k < train[i].route.Count - 1; k++)
                    model.AddLe(model.Sum(train[i].SectionTime[train[i].route[k], train[i].route[k + 1]], D[i][train[i].route[k]]), A[i][train[i].route[k + 1]]);

            // (I) Headway for trains in the same direction
            //(i) Arrival-Arrival headway constraints
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    if (i != j)
                        for (int k = 0; k < train[i].route.Count; k++)
                        {
                            model.AddLe(model.Sum(A[i][k], Parameter.Haa), model.Sum(A[j][k], model.Prod(Max_int, model.Sum(1, model.Prod(-1, xiAA[i][j][k])))));
                            model.AddEq(model.Sum(xiAA[i][j][k], xiAA[j][i][k]), 1);
                        }

            //Departure-Departure headway constraints
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    if (i != j)
                        for (int k = 0; k < train[i].route.Count; k++)
                        {
                            model.AddLe(model.Sum(D[i][k], Parameter.Hdd), model.Sum(D[j][k], model.Prod(Max_int, model.Sum(1, model.Prod(-1, xiDD[i][j][k])))));
                            model.AddEq(model.Sum(xiDD[i][j][k], xiDD[j][i][k]), 1);
                        }

            // Headway constraints for trains in different directions            
            //Arrival-Departure headway constraints
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    if (i != j)
                        for (int k = 0; k < train[i].route.Count; k++)
                        {
                            model.AddLe(model.Sum(A[i][k], Parameter.Had), model.Sum(D[j][k], model.Prod(Max_int, model.Sum(1, model.Prod(-1, xiAD[i][j][k])))));
                            //model.AddEq(model.Sum(xiAD[i, j, k], xiAD[j, i, k]), 1);
                        }

            //Departure-Arrival headway constraints
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    if (i != j)
                        for (int k = 0; k < train[i].route.Count; k++)
                        {
                            model.AddLe(model.Sum(D[i][k], Parameter.Hda), model.Sum(A[j][k], model.Prod(Max_int, model.Sum(1, model.Prod(-1, xiDA[i][j][k])))));
                            //model.AddEq(model.Sum(xiDA[i, j, k], xiDA[j, i, k]), 1);
                        }

            //xiDA[i, j, k] +  xiAD[j, i, k] =1
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    if (i != j)
                        for (int k = 0; k < train[i].route.Count; k++)
                            model.AddEq(model.Sum(xiDA[i][j][k], xiAD[j][i][k]), 1);

            //Tracing constraints
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    if (train[i].trainType == train[j].trainType)
                        for (int k = 0; k < train[i].route.Count - 1; k++)
                            model.AddEq(xiDD[i][j][train[i].route[k]], xiAA[i][j][train[i].route[k + 1]]);

            //Meeting-crossing constraints
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    if (train[i].trainType != train[j].trainType)
                        for (int k = 0; k < train[i].route.Count - 1; k++)
                        {
                            model.AddLe(model.Sum(A[i][train[i].route[k + 1]], Parameter.Had),
                                model.Sum(D[j][train[i].route[k + 1]], model.Prod(Max_int, model.Sum(1, model.Prod(-1, Omega[i][j][train[i].route[k]][train[i].route[k + 1]])))));

                            model.AddEq(model.Sum(Omega[i][j][train[i].route[k]][train[i].route[k + 1]], Omega[j][i][train[i].route[k]][train[i].route[k + 1]]), 1);
                        }
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    if (train[i].trainType != train[j].trainType)
                        for (int k = 0; k < _nstation - 1; k++)
                        {
                            model.AddEq(Omega[i][j][k][k + 1], Omega[i][j][k + 1][k]);
                            for (int n = 0; n < _nstation - 1; n++)
                                model.AddGe(Omega[i][j][k][n], 0);
                        }
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    if (train[i].trainType != train[j].trainType)
                        for (int k = 0; k < _nstation; k++)
                            for (int n = 0; n < _nstation; n++)
                                model.AddGe(Omega[i][j][k][n], 0);


            //Station capacity constraints            
            for (int i = 0; i < _ntrain; i++)
                for (int k = 1; k < _nstation - 1; k++)
                {
                    INumExpr SC = model.IntExpr();
                    for (int c = 0; c < Parameter.C_Station; c++)
                    { SC = model.Sum(SC, Myu[i][k][c]); }
                    model.AddEq(SC, 1);
                }
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    if (i != j)
                        for (int k = 1; k < _nstation - 1; k++)
                            for (int c = 0; c < Parameter.C_Station; c++)
                                model.AddLe(model.Sum(D[i][k], Parameter.Hda),
                                    model.Sum(A[j][k], model.Prod(Max_int, model.Sum(1, model.Prod(-1, xiAA[i][j][k]))),
                                    model.Prod(Max_int, model.Sum(1, model.Prod(-1, Myu[i][k][c]))),
                                    model.Prod(Max_int, model.Sum(1, model.Prod(-1, Myu[j][k][c])))));

            //Departure constraints
            for (int i = 0; i < _ntrain; i++)
                model.AddGe(A[i][train[i].route[0]], train[i].arrival[train[i].route[0]]);

            //model.ExportModel("1.Lp");
        }

        //last_constraint
        internal IRange Import_last_LEconstraint(Cplex model, INumVar[][][] xiAA, INumVar[][][] xiDD, INumVar[][][] xiAD,
       INumVar[][][] xiDA, INumVar[][][] Myu, INumVar[][][][] Omega, double[][][] xiaa, double[][][] xidd,
                double[][][] xiad, double[][][] xida, double[][][] myu, double[][][][] omega, int rhs)
        {
            int _ntrain = xiAA.GetLength(0), _nstation = xiAA[0][0].GetLength(0); CParameter Parameter = new CParameter(); int count = 0;
            INumExpr lbk, lbk_aa, lbk_dd, lbk_ad, lbk_da, lbk_myu, lbk_omega;
            lbk = model.IntExpr(); lbk_aa = model.IntExpr(); lbk_dd = model.IntExpr(); lbk_ad = model.IntExpr();
            lbk_da = model.IntExpr(); lbk_myu = model.IntExpr(); lbk_omega = model.IntExpr();

            //xiAA, _xiDD, _xiAD,  _xiDA
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    if (i != j)
                        for (int k = 0; k < _nstation; k++)
                        {
                            if (xiaa[i][j][k] == 1) { lbk_aa = model.Sum(lbk_aa, xiAA[i][j][k]); count++; }
                            if (xidd[i][j][k] == 1) { lbk_dd = model.Sum(lbk_dd, xiDD[i][j][k]); count++; }

                            if ((i % 2 == 0 && j % 2 == 1) || (i % 2 == 1 && j % 2 == 0))
                            {
                                if (xiad[i][j][k] == 1) { lbk_ad = model.Sum(lbk_ad, xiAD[i][j][k]); count++; }
                                //if (xida[i][j][k] == 1) { lbk_da = model.Sum(lbk_da, xiDA[i][j][k]); count++; }
                            }
                        }

            //Myu
            for (int i = 0; i < Myu.GetLength(0); i++)
                for (int j = 0; j < Myu.GetLength(0); j++)
                    if (i != j)
                        for (int k = 1; k < Myu[i].GetLength(0) - 1; k++)
                            for (int r = 0; r < Parameter.C_Station; r++)
                            { if (myu[i][k][r] == 1) { lbk_myu = model.Sum(lbk_myu, Myu[i][k][r]); count++; } }

            //_Omega
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    if ((i % 2 == 0 && j % 2 == 1) || (j % 2 == 0 && i % 2 == 1))
                        for (int k = 0; k < _nstation - 1; k++)
                        { if (omega[i][j][k][k + 1] == 1) { lbk_omega = model.Sum(lbk_omega, Omega[i][j][k][k + 1]); count++; } }

            lbk = model.Sum(lbk_aa, lbk_dd, lbk_ad, lbk_da, lbk_myu, lbk_omega);

            IRange last_constraint = model.Ge(lbk, (count - rhs));
            return last_constraint;
        }

        internal IRange Import_last_GEconstraint(Cplex model, INumVar[][][] xiAA, INumVar[][][] xiDD, INumVar[][][] xiAD,
      INumVar[][][] xiDA, INumVar[][][] Myu, INumVar[][][][] Omega, double[][][] xiaa, double[][][] xidd,
               double[][][] xiad, double[][][] xida, double[][][] myu, double[][][][] omega, int rhs)
        {
            int _ntrain = xiAA.GetLength(0), _nstation = xiAA[0][0].GetLength(0); CParameter Parameter = new CParameter(); int count = 0;
            INumExpr lbk, lbk_aa, lbk_dd, lbk_ad, lbk_da, lbk_myu, lbk_omega;
            lbk = model.IntExpr(); lbk_aa = model.IntExpr(); lbk_dd = model.IntExpr(); lbk_ad = model.IntExpr();
            lbk_da = model.IntExpr(); lbk_myu = model.IntExpr(); lbk_omega = model.IntExpr();

            //xiAA, _xiDD, _xiAD,  _xiDA
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    if (i != j)
                        for (int k = 0; k < _nstation; k++)
                        {
                            if (xiaa[i][j][k] == 1) { lbk_aa = model.Sum(lbk_aa, xiAA[i][j][k]); count++; }
                            if (xidd[i][j][k] == 1) { lbk_dd = model.Sum(lbk_dd, xiDD[i][j][k]); count++; }

                            if ((i % 2 == 0 && j % 2 == 1) || (i % 2 == 1 && j % 2 == 0))
                            {
                                if (xiad[i][j][k] == 1) { lbk_ad = model.Sum(lbk_ad, xiAD[i][j][k]); count++; }
                                //if (xida[i][j][k] == 1) { lbk_da = model.Sum(lbk_da, xiDA[i][j][k]); count++; }
                            }
                        }

            //Myu
            for (int i = 0; i < Myu.GetLength(0); i++)
                for (int j = 0; j < Myu.GetLength(0); j++)
                    if (i != j)
                        for (int k = 1; k < Myu[i].GetLength(0) - 1; k++)
                            for (int r = 0; r < Parameter.C_Station; r++)
                            { if (myu[i][k][r] == 1) { lbk_myu = model.Sum(lbk_myu, Myu[i][k][r]); count++; } }

            //_Omega
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    if ((i % 2 == 0 && j % 2 == 1) || (j % 2 == 0 && i % 2 == 1))
                        for (int k = 0; k < _nstation - 1; k++)
                        { if (omega[i][j][k][k + 1] == 1) { lbk_omega = model.Sum(lbk_omega, Omega[i][j][k][k + 1]); count++; } }

            lbk = model.Sum(lbk_aa, lbk_dd, lbk_ad, lbk_da, lbk_myu, lbk_omega);

            IRange last_constraint = model.Le(lbk, (count - rhs));
            return last_constraint;
        }

        //update initial solution
        internal void Update_reference_solution(Cplex model, INumVar[][] A, INumVar[][] D, INumVar[][][] xiAA, INumVar[][][] xiDD, INumVar[][][] xiAD,
                INumVar[][][] xiDA, INumVar[][][] Myu, INumVar[][][][] Omega, double[][] a, double[][] d, double[][][] xiaa, double[][][] xidd,
                double[][][] xiad, double[][][] xida, double[][][] myu, double[][][][] omega)
        {
            int _ntrain = A.GetLength(0), _nstation = A[0].GetLength(0);

            //A,D
            for (int i = 0; i < A.GetLength(0); i++)
            { a[i] = model.GetValues(A[i]); d[i] = model.GetValues(D[i]); }

            //xiAA, _xiDD, _xiAD,  _xiDA
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    if (i != j)
                    {
                        xiaa[i][j] = model.GetValues(xiAA[i][j]); xidd[i][j] = model.GetValues(xiDD[i][j]);
                        xiad[i][j] = model.GetValues(xiAD[i][j]); xida[i][j] = model.GetValues(xiDA[i][j]);
                    }

            //Myu
            for (int i = 0; i < Myu.GetLength(0); i++)
                for (int j = 0; j < Myu.GetLength(0); j++)
                    if (i != j)
                        for (int k = 1; k < Myu[i].GetLength(0) - 1; k++)
                        { myu[i][k] = model.GetValues(Myu[i][k]); }

            //_Omega
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    if ((i % 2 == 0 && j % 2 == 1) || (j % 2 == 0 && i % 2 == 1))
                        for (int k = 0; k < _nstation - 1; k++)
                        { omega[i][j][k] = model.GetValues(Omega[i][j][k]); }
        }

        //insert a initial solution
        internal class Solve : Cplex.SolveCallback
        {
            internal bool _done = false;
            internal INumVar[][] _A, _D;
            internal INumVar[][][] _xiAA, _xiDD, _xiAD, _xiDA, _Myu;
            internal INumVar[][][][] _Omega;

            internal double[][] _a, _d;
            internal double[][][] _xiaa, _xidd, _xiad, _xida, _myu;
            internal double[][][][] _omega;

            internal Solve(INumVar[][] A, INumVar[][] D, INumVar[][][] xiAA, INumVar[][][] xiDD, INumVar[][][] xiAD,
                INumVar[][][] xiDA, INumVar[][][] Myu, INumVar[][][][] Omega, double[][] a, double[][] d, double[][][] xiaa, double[][][] xidd,
                double[][][] xiad, double[][][] xida, double[][][] myu, double[][][][] omega)
            {
                _A = A; _D = D; _xiAA = xiAA; _xiDD = xiDD; _xiAD = xiAD; _xiDA = xiDA; _Myu = Myu; _Omega = Omega;
                _a = a; _d = d; _xiaa = xiaa; _xidd = xidd; _xiad = xiad; _xida = xida; _myu = myu; _omega = omega;
            }

            //SetVectors(_x, _vars, null, null); equals SetStart
            //x  An array of starting values for the variables specified in var.  
            //varType:  ILOG.Concert.INumVar []; The array of variables for which to set starting point information.
            public override void Main()
            {
                if (!_done)
                {
                    int _ntrain = _A.GetLength(0), _nstation = _A[0].GetLength(0);
                    //_A,_D
                    for (int i = 0; i < _A.GetLength(0); i++)
                    { SetStart(_a[i], _A[i], null, null); SetStart(_d[i], _D[i], null, null); }

                    //xiAA, _xiDD, _xiAD,  _xiDA
                    for (int i = 0; i < _ntrain; i++)
                        for (int j = 0; j < _ntrain; j++)
                            if (i != j)
                            {
                                SetStart(_xiaa[i][j], _xiAA[i][j], null, null); SetStart(_xidd[i][j], _xiDD[i][j], null, null);
                                SetStart(_xiad[i][j], _xiAD[i][j], null, null); SetStart(_xida[i][j], _xiDA[i][j], null, null);
                            }

                    //Myu
                    for (int i = 0; i < _Myu.GetLength(0); i++)
                        for (int j = 0; j < _Myu.GetLength(0); j++)
                            if (i != j)
                                for (int k = 1; k < _Myu[i].GetLength(0) - 1; k++)
                                { SetStart(_myu[i][k], _Myu[i][k], null, null); }

                    //_Omega
                    for (int i = 0; i < _ntrain; i++)
                        for (int j = 0; j < _ntrain; j++)
                            if ((i % 2 == 0 && j % 2 == 1) || (j % 2 == 0 && i % 2 == 1))
                                for (int k = 0; k < _nstation - 1; k++)
                                { SetStart(_omega[i][j][k], _Omega[i][j][k], null, null); }

                    _done = true;
                    Console.WriteLine();
                    Console.WriteLine(" ************ Initial solution is read into Root Node ! ***********");
                    Console.WriteLine();
                }
            }
        }

        // internal static INumVar[][] populateByRow(IMPModeler model, IRange[] row, List<Ctrain> train, List<Crailway_station> station)
        internal void Cplex_Pure(List<CTrain> train, List<CRailwayStation> station, List<CRailwaySection> section, StreamWriter CPLEX_output, int _nset)
        {
            Cplex model = new Cplex();
            CParameter Parameter = new CParameter();
            INumVar[][] vax = new INumVar[6][];
            int Max_int = 10000000;
            int _ntrain = train.Count; int _nstation = station.Count;

            INumVar[][] A = new INumVar[_ntrain][];//, _nstation]; //arrival time
            INumVar[][] D = new INumVar[_ntrain][];//, _nstation]; //departure time
            INumVar[][][] xiAA = new INumVar[_ntrain][][];//, _ntrain, _nstation];
            INumVar[][][] xiDD = new INumVar[_ntrain][][];//, _ntrain, _nstation];
            INumVar[][][] xiAD = new INumVar[_ntrain][][];//, _ntrain, _nstation];
            INumVar[][][] xiDA = new INumVar[_ntrain][][];//, _ntrain, _nstation];            
            INumVar[][][] Myu = new INumVar[_ntrain][][]; //_nstation, station_capacity
            INumVar[][][][] Omega = new INumVar[_ntrain][][][];//, _ntrain, _nstation, _nstation];
            NumVarType[] train_IntType = new NumVarType[_ntrain];
            for (int i = 0; i < _ntrain; i++)
            { train_IntType[i] = NumVarType.Int; }

            NumVarType[] station_IntType = new NumVarType[_nstation];
            for (int k = 0; k < _nstation; k++)
            { station_IntType[k] = NumVarType.Int; }

            NumVarType[] C_IntType = new NumVarType[Parameter.C_Station];
            for (int c = 0; c < Parameter.C_Station; c++)
            { C_IntType[c] = NumVarType.Int; }

            double[] lb_train = new double[_ntrain], up_train = new double[_ntrain];
            for (int i = 0; i < _ntrain; i++) { lb_train[i] = 0.0; up_train[i] = System.Double.MaxValue; }

            double[] lb_station = new double[_nstation], up_station = new double[_nstation];
            for (int k = 0; k < _nstation; k++) { lb_station[k] = 0.0; up_station[k] = System.Double.MaxValue; }

            double[] lb_c = new double[Parameter.C_Station], up_c = new double[Parameter.C_Station];
            for (int c = 0; c < Parameter.C_Station; c++) { lb_c[c] = 0.0; up_c[c] = 1.0; }

            //A,D                 
            for (int i = 0; i < _ntrain; i++)
            {
                A[i] = model.NumVarArray(_nstation, lb_station, up_station, station_IntType);
                D[i] = model.NumVarArray(_nstation, lb_station, up_station, station_IntType);
            }

            //xiaa, xidd, xiad, xida
            for (int i = 0; i < _ntrain; i++)
            {
                xiAA[i] = new INumVar[_ntrain][];
                xiDD[i] = new INumVar[_ntrain][];
                xiAD[i] = new INumVar[_ntrain][];
                xiDA[i] = new INumVar[_ntrain][];
                for (int j = 0; j < _ntrain; j++)
                {
                    xiAA[i][j] = model.NumVarArray(_nstation, lb_station, up_station, station_IntType);
                    xiDD[i][j] = model.NumVarArray(_nstation, lb_station, up_station, station_IntType);
                    xiAD[i][j] = model.NumVarArray(_nstation, lb_station, up_station, station_IntType);
                    xiDA[i][j] = model.NumVarArray(_nstation, lb_station, up_station, station_IntType);

                    for (int k = 0; k < _nstation; k++)
                    {
                        xiAA[i][j][k] = model.IntVar(0, 1);
                        xiDD[i][j][k] = model.IntVar(0, 1);
                        xiAD[i][j][k] = model.IntVar(0, 1);
                        xiDA[i][j][k] = model.IntVar(0, 1);
                    }
                }
            }

            //myu
            for (int i = 0; i < _ntrain; i++)
            {
                Myu[i] = new INumVar[_nstation][];
                for (int k = 0; k < _nstation; k++)
                {
                    Myu[i][k] = model.NumVarArray(Parameter.C_Station, lb_c, up_c, C_IntType);
                    for (int c = 0; c < Parameter.C_Station; c++)
                        Myu[i][k][c] = model.IntVar(0, 1);
                }
            }

            //omega
            for (int i = 0; i < _ntrain; i++)
            {
                Omega[i] = new INumVar[_ntrain][][];
                for (int j = 0; j < _ntrain; j++)
                {
                    Omega[i][j] = new INumVar[_nstation][];
                    for (int k = 0; k < _nstation; k++)
                    {
                        Omega[i][j][k] = model.NumVarArray(_nstation, lb_station, up_station, station_IntType);
                        for (int n = 0; n < _nstation; n++)
                        { Omega[i][j][k][n] = model.IntVar(0, 1); }
                    }
                }
            }

            INumExpr objective = model.IntExpr();

            int free_total = 0;
            for (int i = 0; i < train.Count(); i++)
                free_total += train[i].free_departure[train[i].route[train[i].route.Count() - 1]];

            //double free_total = 0;
            for (int i = 0; i < _ntrain; i++)
            {
                objective = model.Sum(D[i][train[i].route[train[i].route.Count - 1]], objective);
                //free_total = free_total + train[i].free_run_time;
                //objective = model.Sum(model.Prod(train[i].cost_factor,
                //    model.Sum(D[i][train[i].route[train[i].route.Count - 1]], -1 * train[i].free_departure[train[i].route[train[i].route.Count - 1]])), objective);
            }
            //objective = model.Prod(objective, 1 / free_total);

            objective = model.Sum(objective, -1 * free_total);

            IObjective obj_function = model.Minimize(objective);
            IObjective no_function = model.Minimize();

            model.Add(obj_function);
            //model.Add(no_function);

            //a i,k + t i,k ≤ d i,k ∀i ∈ N; k ∈ R i 
            for (int i = 0; i < _ntrain; i++)
                for (int k = 0; k < train[i].route.Count; k++)
                    model.AddLe(model.Sum(train[i].dwellingtime[train[i].route[k]], A[i][train[i].route[k]]), D[i][train[i].route[k]]);

            //d i,k + t_{i}^{k,k^+} ≤ a i,k^+  ∀i ∈ N; k ∈ R i \Des(i) 
            for (int i = 0; i < _ntrain; i++)
                for (int k = 0; k < train[i].route.Count - 1; k++)
                    model.AddLe(model.Sum(train[i].SectionTime[train[i].route[k], train[i].route[k + 1]], D[i][train[i].route[k]]), A[i][train[i].route[k + 1]]);

            // (I) Headway for trains in the same direction
            //(i) Arrival-Arrival headway constraints
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    if (i != j)
                        for (int k = 0; k < train[i].route.Count; k++)
                        {
                            model.AddLe(model.Sum(A[i][k], Parameter.Haa), model.Sum(A[j][k], model.Prod(Max_int, model.Sum(1, model.Prod(-1, xiAA[i][j][k])))));
                            model.AddEq(model.Sum(xiAA[i][j][k], xiAA[j][i][k]), 1);
                        }

            //Departure-Departure headway constraints
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    if (i != j)
                        for (int k = 0; k < train[i].route.Count; k++)
                        {
                            model.AddLe(model.Sum(D[i][k], Parameter.Hdd), model.Sum(D[j][k], model.Prod(Max_int, model.Sum(1, model.Prod(-1, xiDD[i][j][k])))));
                            model.AddEq(model.Sum(xiDD[i][j][k], xiDD[j][i][k]), 1);
                        }

            // Headway constraints for trains in different directions            
            //Arrival-Departure headway constraints
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    if (i != j)
                        for (int k = 0; k < train[i].route.Count; k++)
                        {
                            model.AddLe(model.Sum(A[i][k], Parameter.Had), model.Sum(D[j][k], model.Prod(Max_int, model.Sum(1, model.Prod(-1, xiAD[i][j][k])))));
                            //model.AddEq(model.Sum(xiAD[i, j, k], xiAD[j, i, k]), 1);
                        }

            //Departure-Arrival headway constraints
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    if (i != j)
                        for (int k = 0; k < train[i].route.Count; k++)
                        {
                            model.AddLe(model.Sum(D[i][k], Parameter.Hda), model.Sum(A[j][k], model.Prod(Max_int, model.Sum(1, model.Prod(-1, xiDA[i][j][k])))));
                            //model.AddEq(model.Sum(xiDA[i, j, k], xiDA[j, i, k]), 1);
                        }

            //xiDA[i, j, k] +  xiAD[j, i, k] =1
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    if (i != j)
                        for (int k = 0; k < train[i].route.Count; k++)
                            model.AddEq(model.Sum(xiDA[i][j][k], xiAD[j][i][k]), 1);

            //Tracing constraints
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    if (train[i].trainType == train[j].trainType)
                        for (int k = 0; k < train[i].route.Count - 1; k++)
                            model.AddEq(xiDD[i][j][train[i].route[k]], xiAA[i][j][train[i].route[k + 1]]);

            //Meeting-crossing constraints
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    if (train[i].trainType != train[j].trainType)
                        for (int k = 0; k < train[i].route.Count - 1; k++)
                        {
                            model.AddLe(model.Sum(A[i][train[i].route[k + 1]], Parameter.Had),
                                model.Sum(D[j][train[i].route[k + 1]], model.Prod(Max_int, model.Sum(1, model.Prod(-1, Omega[i][j][train[i].route[k]][train[i].route[k + 1]])))));

                            model.AddEq(model.Sum(Omega[i][j][train[i].route[k]][train[i].route[k + 1]], Omega[j][i][train[i].route[k]][train[i].route[k + 1]]), 1);
                        }
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    if (train[i].trainType != train[j].trainType)
                        for (int k = 0; k < _nstation - 1; k++)
                            model.AddEq(Omega[i][j][k][k + 1], Omega[i][j][k + 1][k]);
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    if (train[i].trainType != train[j].trainType)
                        for (int k = 0; k < _nstation; k++)
                            for (int n = 0; n < _nstation; n++)
                                model.AddGe(Omega[i][j][k][n], 0);

            //Station capacity constraints            
            for (int i = 0; i < _ntrain; i++)
                for (int k = 1; k < _nstation - 1; k++)
                {
                    INumExpr SC = model.IntExpr();
                    for (int c = 0; c < Parameter.C_Station; c++)
                    { SC = model.Sum(SC, Myu[i][k][c]); }
                    model.AddEq(SC, 1);
                }
            for (int i = 0; i < _ntrain; i++)
                for (int j = 0; j < _ntrain; j++)
                    if (i != j)
                        for (int k = 1; k < _nstation - 1; k++)
                            for (int c = 0; c < Parameter.C_Station; c++)
                                model.AddLe(model.Sum(D[i][k], Parameter.Hda),
                                    model.Sum(A[j][k], model.Prod(Max_int, model.Sum(1, model.Prod(-1, xiAA[i][j][k]))),
                                    model.Prod(Max_int, model.Sum(1, model.Prod(-1, Myu[i][k][c]))),
                                    model.Prod(Max_int, model.Sum(1, model.Prod(-1, Myu[j][k][c])))));

            //Departure constraints
            for (int i = 0; i < _ntrain; i++)
                model.AddGe(A[i][train[i].route[0]], train[i].arrival[train[i].route[0]]);

            //model.ExportModel("2.Lp");

            //***********************************************
            #region
            /*
            double[][] a, d; double[][][] xiaa, xidd, xiad, xida, myu; double[][][][] omega;
            a = new double[_ntrain][]; d = new double[_ntrain][]; xiaa = new double[_ntrain][][]; xidd = new double[_ntrain][][];
            xiad = new double[_ntrain][][]; xida = new double[_ntrain][][]; myu = new double[_ntrain][][]; omega = new double[_ntrain][][][];
            for (int i = 0; i < _ntrain; i++)
            {
                a[i] = new double[_nstation]; d[i] = new double[_nstation]; xiaa[i] = new double[_ntrain][]; xidd[i] = new double[_ntrain][];
                xiad[i] = new double[_ntrain][]; xida[i] = new double[_ntrain][]; myu[i] = new double[_nstation][]; omega[i] = new double[_ntrain][][];
                for (int j = 0; j < _ntrain; j++)
                {
                    xiaa[i][j] = new double[_nstation]; xidd[i][j] = new double[_nstation]; xiad[i][j] = new double[_nstation]; xida[i][j] = new double[_nstation];
                    omega[i][j] = new double[_nstation][];
                    for (int k = 0; k < _nstation; k++)
                    { omega[i][j][k] = new double[_nstation]; }
                }
                for (int k = 0; k < _nstation; k++)
                { myu[i][k] = new double[Parameter.C_Station]; }
            }*/
            #endregion

            string defaultPath = System.IO.Directory.GetCurrentDirectory().ToString();//读取txt文件address
            DirectoryInfo Experiment = new DirectoryInfo(@defaultPath + "\\CPLEX_Statistic_Result"); //save this set of experiments in this file
            if (!Experiment.Exists)
                Experiment.Create();
            string cplex_outputdatapath = System.IO.Path.GetDirectoryName(@defaultPath + "\\CPLEX_Statistic_Result\\");

            #region
            /*
            //import initial solution
            Initial_solution(train, station, a, d, xiaa, xidd, xiad, xida, myu, omega);
            model.Use(new Solve(A, D, xiAA, xiDD, xiAD, xiDA, Myu, Omega, a, d, xiaa, xidd, xiad, xida, myu, omega));
            // ***********************************************
             */
            #endregion

            double obj = 0.0, lower_bound = 0.0;
            double cplex_time_start = model.CplexTime;
            model.SetParam(Cplex.DoubleParam.TiLim, Parameter.total_time_limit); //set compute time

            //model.SetParam(Cplex.BooleanParam.LBHeur, true);
            //model.SetParam(Cplex.LongParam.RINSHeur, 20); // the frequency to use RINS

            Stopwatch sw = new Stopwatch();
            sw.Start();
            try
            {
                TextWriter cplex_log = File.CreateText(@cplex_outputdatapath + "\\cplex_pure_log_" + train.Count + "_trains_" + _nset + "_case.txt");
                model.SetOut(cplex_log);

                //model.SetOut(Console.Error);
                if (model.Solve())
                {
                    if (model.GetStatus().Equals(Cplex.Status.Infeasible))
                    {
                        System.Console.WriteLine("No Solution");
                        return;
                    }

                    //if (model.GetStatus().Equals(Cplex.Status.Optimal))
                    //{ model.WriteSolution("cplex_pure_solution_" + train.Count + "_trains.txt"); }
                    System.Console.WriteLine("Has Solution");

                    obj = model.ObjValue;
                    lower_bound = model.BestObjValue;
                }

                //int free_run_time_total = 0;
                //for (int i = 0; i < train.Count(); i++)
                //    free_total += train[i].free_departure[train[i].route[train[i].route.Count() - 1]];
                //obj = obj - free_total;

                sw.Stop();
                int cplex_cpu_Time;
                cplex_cpu_Time = Int32.Parse(sw.ElapsedMilliseconds.ToString()) / 1000; // unit is sec
              
                CPLEX_output.WriteLine(_nset + "\t" + train.Count + "\t" + obj + "\t" + lower_bound + "\t" + cplex_cpu_Time);

                for (int i = 0; i < _ntrain; i++)
                    for (int k = 0; k < _nstation; k++)
                    {
                        train[i].arrival[k] = (int)model.GetValue(A[i][k]);
                        train[i].departure[k] = (int)model.GetValue(D[i][k]);
                    }

                COutput_Timetable_Result output_result = new COutput_Timetable_Result(train, station, section, "CPLEX", _nset);
                cplex_log.Close();
            }

            catch (ILOG.Concert.Exception exc)
            {
                System.Console.WriteLine("Concert exception '" + exc + "' caught");
            }
            model.End();

            //System.Console.Write(" Time is over. Press any key to exit ...");
            //Console.ReadKey(true);
            //System.Environment.Exit(0);  // end this project
        }
    }
}
