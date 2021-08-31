using System;
using System.Collections.Generic;
using System.Text;
using Gurobi;

namespace LithographyAreaValidation.Solvers
{
    public class GurobiSolver
    {
        public void layer_Machine_Solver()
        {
            
        }

        public void mip1_cs()
        {
            try
            {

                // Create an empty environment, set options and start
                GRBEnv env = new GRBEnv(true);
                env.Set("LogFile", "mip1.log");
                env.Start();

                // Create empty model
                GRBModel model = new GRBModel(env);

                // Create variables
                GRBVar x = model.AddVar(0.0, 1.0, 1.0, GRB.BINARY, "x");
                GRBVar y = model.AddVar(0.0, 1.0, 1.0, GRB.BINARY, "y");
                GRBVar z = model.AddVar(0.0, 1.0, 1.0, GRB.BINARY, "z");

                // Set objective: maximize x + y + 2 z
                model.SetObjective(x + y + 2 * z, GRB.MAXIMIZE);

                // Add constraint: x + 2 y + 3 z <= 4
                model.AddConstr(x + 2 * y + 3 * z <= 4.0, "c0");

                // Add constraint: x + y >= 1
                model.AddConstr(x + y >= 1.0, "c1");

                // Optimize model
                model.Optimize();

                Console.WriteLine(x.VarName + " " + x.X);
                Console.WriteLine(y.VarName + " " + y.X);
                Console.WriteLine(z.VarName + " " + z.X);

                Console.WriteLine("Obj: " + model.ObjVal);

                // Dispose of model and env
                model.Dispose();
                env.Dispose();

            }
            catch (GRBException e)
            {
                Console.WriteLine("Error code: " + e.ErrorCode + ". " + e.Message);
            }
        }
        public void ILP_Janssen()
        {
            try
            {
                // Number of plants and warehouses
                int nJobs = 750;
                int nMachines = 12;

                double[,] processingTime =
                    new double[nMachines, nJobs];

                for (int i = 0; i < nMachines; ++i)
                {
                    for (int j = 0; j < nJobs; ++j)
                    {
                        processingTime[i, j] = new Random().Next(1000, 1500);
                    }
                }

                // Number of plants and warehouses
                //int nJobs = 20;
                //int nMachines = 3;

                //double[,] processingTime =
                //    new double[nMachines, nJobs];

                //Random Rand = new Random(1);

                //for (int i = 0; i < nMachines; ++i)
                //{
                //    if (i==0)
                //    {
                //        for (int j = 0; j < nJobs; ++j)
                //        {
                //            processingTime[i, j] = Rand.Next(100, 201);
                //        }
                //    }
                //    else if (i==1)
                //    {
                //        for (int j = 0; j < nJobs; ++j)
                //        {
                //            processingTime[i, j] = Rand.Next(100, 201);
                //        }
                //    }
                //    else if (i == 2)
                //    {
                //        for (int j = 0; j < nJobs; ++j)
                //        {
                //            processingTime[i, j] = Rand.Next(100, 201);
                //        }
                //    }
                //}

                // Create an empty environment, set options and start
                GRBEnv env = new GRBEnv(true);
                env.Set("LogFile", "mip1.log");
                env.Start();

                // Create empty model
                GRBModel model = new GRBModel(env);

                // Decision Variable
                GRBVar[,,] x = new GRBVar[nMachines, nJobs,nJobs];

                for (int i = 0; i < nMachines; ++i)
                {
                    for (int j = 0; j < nJobs; ++j)
                    {
                        for (int k = 0; k < nJobs; ++k)
                        {
                            x[i, j, k] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, "x");
                        }
                    }
                }

                //Set objective:
                GRBLinExpr exp1 = 0.0;

                for (int i = 0; i < nMachines; ++i)
                {
                    for (int j = 0; j < nJobs; ++j)
                    {
                        for (int k = 0; k < nJobs; ++k)
                        {
                            GRBLinExpr exp2 = (k + 1) * processingTime[i, j] * x[i, j, k];
                            exp1.Add(exp2);
                        }
                    }
                }

                model.SetObjective(exp1, GRB.MINIMIZE);

                // Constraints
                for (int j = 0; j < nJobs; ++j)
                {
                    GRBLinExpr exp3 = 0.0;

                    for (int i = 0; i < nMachines; ++i)
                    {
                        for (int k = 0; k < nJobs; ++k)
                        {
                            exp3.AddTerm(1.0, x[i, j, k]);
                        }
                    }
                    model.AddConstr(exp3 == 1.0, "c1");
                }

                for (int i = 0; i < nMachines; ++i)
                {
                    for (int k = 0; k < nJobs; ++k)
                    {
                        GRBLinExpr exp4 = 0.0;
                        for (int j = 0; j < nJobs; ++j)
                        {
                            exp4.AddTerm(1.0,x[i, j, k]);
                        }
                        model.AddConstr(exp4 <= 1.0, "c2");
                    }
                }

                // Solve
                model.Optimize();

                for (int i = 0; i < nMachines; ++i)
                {
                    double totalMachineTime = 0.0;
                    for (int j = 0; j < nJobs; ++j)
                    {
                        for (int k = 0; k < nJobs; ++k)
                        {
                            if (x[i, j, k].X == 1.0)
                            {
                                totalMachineTime += processingTime[i, j];
                                Console.WriteLine($"({i},{j},{k})" + "ProcessingTime: " + processingTime[i, j]);
                            }
                        }
                    }
                    Console.WriteLine(totalMachineTime);
                }

                Console.WriteLine("Obj: " + model.ObjVal);

                // Dispose of model and env
                model.Dispose();
                env.Dispose();

            }
            catch (GRBException e)
            {
                Console.WriteLine("Error code: " + e.ErrorCode + ". " + e.Message);
            }
        }

        public void ILP_Janssen_ExtraParameter(double parameter)
        {
            try
            {
                // Number of plants and warehouses
                //int nJobs = 20;
                //int nMachines = 12;

                //double[,] processingTime =
                //    new double[nMachines,nJobs];

                //for (int i = 0; i<nMachines; ++i)
                //{
                //    for (int j = 0; j<nJobs; ++j)
                //    {
                //        processingTime[i, j] = new Random().Next(100,201);
                //    }
                //}

                // Number of plants and warehouses
                int nJobs = 20;
                int nMachines = 3;

                double[,] processingTime =
                    new double[nMachines, nJobs];

                Random Rand = new Random(1);

                for (int i = 0; i < nMachines; ++i)
                {
                    if (i == 0)
                    {
                        for (int j = 0; j < nJobs; ++j)
                        {
                            processingTime[i, j] = Rand.Next(100, 201);
                        }
                    }
                    else if (i == 1)
                    {
                        for (int j = 0; j < nJobs; ++j)
                        {
                            processingTime[i, j] = Rand.Next(100, 201);
                        }
                    }
                    else if (i == 2)
                    {
                        for (int j = 0; j < nJobs; ++j)
                        {
                            processingTime[i, j] = Rand.Next(100, 201);
                        }
                    }
                }

                // Create an empty environment, set options and start
                GRBEnv env = new GRBEnv(true);
                env.Set("LogFile", "mip1.log");
                env.Start();

                // Create empty model
                GRBModel model = new GRBModel(env);

                // Decision Variable
                GRBVar[,,] x = new GRBVar[nMachines, nJobs, nJobs];

                for (int i = 0; i < nMachines; ++i)
                {
                    for (int j = 0; j < nJobs; ++j)
                    {
                        for (int k = 0; k < nJobs; ++k)
                        {
                            x[i, j, k] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, "x");
                        }
                    }
                }

                //Set objective:
                GRBLinExpr exp1 = 0.0;

                for (int i = 0; i < nMachines; ++i)
                {
                    for (int j = 0; j < nJobs; ++j)
                    {
                        for (int k = 0; k < nJobs; ++k)
                        {
                            GRBLinExpr exp2 = (k * parameter + 1) * processingTime[i, j] * x[i, j, k];
                            exp1.Add(exp2);
                        }
                    }
                }

                model.SetObjective(exp1, GRB.MINIMIZE);

                // Constraints
                for (int j = 0; j < nJobs; ++j)
                {
                    GRBLinExpr exp3 = 0.0;

                    for (int i = 0; i < nMachines; ++i)
                    {
                        for (int k = 0; k < nJobs; ++k)
                        {
                            exp3.AddTerm(1.0, x[i, j, k]);
                        }
                    }
                    model.AddConstr(exp3 == 1.0, "c1");
                }

                for (int i = 0; i < nMachines; ++i)
                {
                    for (int k = 0; k < nJobs; ++k)
                    {
                        GRBLinExpr exp4 = 0.0;
                        for (int j = 0; j < nJobs; ++j)
                        {
                            exp4.AddTerm(1.0, x[i, j, k]);
                        }
                        model.AddConstr(exp4 <= 1.0, "c2");
                    }
                }

                // Solve
                model.Optimize();

                for (int i = 0; i < nMachines; ++i)
                {
                    double totalMachineTime = 0.0;
                    for (int j = 0; j < nJobs; ++j)
                    {
                        for (int k = 0; k < nJobs; ++k)
                        {
                            if (x[i, j, k].X == 1.0)
                            {
                                totalMachineTime += processingTime[i, j];
                                Console.WriteLine($"({i},{j},{k})" + "ProcessingTime: " + processingTime[i, j]);
                            }
                        }
                    }
                    Console.WriteLine(totalMachineTime);
                }

                Console.WriteLine("Obj: " + model.ObjVal);

                // Dispose of model and env
                model.Dispose();
                env.Dispose();

            }
            catch (GRBException e)
            {
                Console.WriteLine("Error code: " + e.ErrorCode + ". " + e.Message);
            }
        }

        public void ILP_MinProductionTime()
        {
            try
            {
                // Number of plants and warehouses
                //int nJobs = 20;
                //int nMachines = 12;

                //double[,] processingTime =
                //    new double[nMachines,nJobs];

                //for (int i = 0; i<nMachines; ++i)
                //{
                //    for (int j = 0; j<nJobs; ++j)
                //    {
                //        processingTime[i, j] = new Random().Next(100,201);
                //    }
                //}

                // Number of plants and warehouses
                int nJobs = 20;
                int nMachines = 3;

                double[,] processingTime =
                    new double[nMachines, nJobs];

                Random Rand = new Random(1);

                for (int i = 0; i < nMachines; ++i)
                {
                    if (i == 0)
                    {
                        for (int j = 0; j < nJobs; ++j)
                        {
                            processingTime[i, j] = Rand.Next(100, 201);
                        }
                    }
                    else if (i == 1)
                    {
                        for (int j = 0; j < nJobs; ++j)
                        {
                            processingTime[i, j] = Rand.Next(100, 201);
                        }
                    }
                    else if (i == 2)
                    {
                        for (int j = 0; j < nJobs; ++j)
                        {
                            processingTime[i, j] = Rand.Next(100, 201);
                        }
                    }
                }

                // Create an empty environment, set options and start
                GRBEnv env = new GRBEnv(true);
                env.Set("LogFile", "mip1.log");
                env.Start();

                // Create empty model
                GRBModel model = new GRBModel(env);

                // Decision Variable
                GRBVar[,,] x = new GRBVar[nMachines, nJobs, nJobs];

                for (int i = 0; i < nMachines; ++i)
                {
                    for (int j = 0; j < nJobs; ++j)
                    {
                        for (int k = 0; k < nJobs; ++k)
                        {
                            x[i, j, k] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, "x");
                        }
                    }
                }

                //Set objective:
                GRBLinExpr exp1 = 0.0;
                for (int i = 0; i < nMachines; ++i)
                {
                    for (int j = 0; j < nJobs; ++j)
                    {
                        for (int k = 0; k < nJobs; ++k)
                        {
                            GRBLinExpr exp2 = processingTime[i, j] * x[i, j, k];
                            exp1.Add(exp2);
                        }
                    }
                }

                model.SetObjective(exp1, GRB.MINIMIZE);

                // Constraints
                for (int j = 0; j < nJobs; ++j)
                {
                    GRBLinExpr exp3 = 0.0;

                    for (int i = 0; i < nMachines; ++i)
                    {
                        for (int k = 0; k < nJobs; ++k)
                        {
                            exp3.AddTerm(1.0, x[i, j, k]);
                        }
                    }
                    model.AddConstr(exp3 == 1.0, "c1");
                }

                for (int i = 0; i < nMachines; ++i)
                {
                    for (int k = 0; k < nJobs; ++k)
                    {
                        GRBLinExpr exp4 = 0.0;
                        for (int j = 0; j < nJobs; ++j)
                        {
                            exp4.AddTerm(1.0, x[i, j, k]);
                        }
                        model.AddConstr(exp4 <= 1.0, "c2");
                    }
                }

                // Solve
                model.Optimize();

                for (int i = 0; i < nMachines; ++i)
                {
                    double totalMachineTime = 0.0;
                    for (int j = 0; j < nJobs; ++j)
                    {
                        for (int k = 0; k < nJobs; ++k)
                        {
                            if (x[i, j, k].X == 1.0)
                            {
                                totalMachineTime += processingTime[i, j];
                                Console.WriteLine($"({i},{j},{k})" + "ProcessingTime: " + processingTime[i, j]);
                            }
                        }
                    }
                    Console.WriteLine(totalMachineTime);
                }

                Console.WriteLine("Obj: " + model.ObjVal);

                // Dispose of model and env
                model.Dispose();
                env.Dispose();

            }
            catch (GRBException e)
            {
                Console.WriteLine("Error code: " + e.ErrorCode + ". " + e.Message);
            }
        }

        public void ILP_Cmax()
        {
            try
            {
                // Number of plants and warehouses
                //int nJobs = 20;
                //int nMachines = 12;

                //double[,] processingTime =
                //    new double[nMachines,nJobs];

                //for (int i = 0; i<nMachines; ++i)
                //{
                //    for (int j = 0; j<nJobs; ++j)
                //    {
                //        processingTime[i, j] = new Random().Next(100,201);
                //    }
                //}

                // Number of plants and warehouses
                int nJobs = 20;
                int nMachines = 3;

                double[,] processingTime =
                    new double[nMachines, nJobs];

                Random Rand = new Random(1);

                for (int i = 0; i < nMachines; ++i)
                {
                    if (i == 0)
                    {
                        for (int j = 0; j < nJobs; ++j)
                        {
                            processingTime[i, j] = Rand.Next(100, 201);
                        }
                    }
                    else if (i == 1)
                    {
                        for (int j = 0; j < nJobs; ++j)
                        {
                            processingTime[i, j] = Rand.Next(100, 201);
                        }
                    }
                    else if (i == 2)
                    {
                        for (int j = 0; j < nJobs; ++j)
                        {
                            processingTime[i, j] = Rand.Next(100, 201);
                        }
                    }
                }

                // Create an empty environment, set options and start
                GRBEnv env = new GRBEnv(true);
                env.Set("LogFile", "mip1.log");
                env.Start();

                // Create empty model
                GRBModel model = new GRBModel(env);

                // Decision Variable
                GRBVar[,,] x = new GRBVar[nMachines, nJobs, nJobs];

                for (int i = 0; i < nMachines; ++i)
                {
                    for (int j = 0; j < nJobs; ++j)
                    {
                        for (int k = 0; k < nJobs; ++k)
                        {
                            x[i, j, k] = model.AddVar(0.0, 1.0, 0.0, GRB.BINARY, "x");
                        }
                    }
                }

                GRBVar cMaxVar = model.AddVar(0.0, GRB.INFINITY, 0.0, GRB.CONTINUOUS, "cMaxVar");

                //Set objective:
                GRBLinExpr exp1 = 0.0;
                GRBLinExpr[] c = new GRBLinExpr[nMachines];

                for (int i = 0; i < nMachines; ++i)
                {
                    c[i] = 0.0;
                }

                for (int i = 0; i < nMachines; ++i)
                {
                    for (int j = 0; j < nJobs; ++j)
                    {
                        for (int k = 0; k < nJobs; ++k)
                        {
                            GRBLinExpr exp2 = (k + 1) * processingTime[i, j] * x[i, j, k];
                            exp1.Add(exp2);

                            c[i] += processingTime[i, j] * x[i, j, k];
                        }
                    }
                }

                GRBLinExpr cMaxExpr = cMaxVar;

                model.SetObjective(cMaxExpr, GRB.MINIMIZE);


                for (int i = 0; i < nMachines; ++i)
                {
                    model.AddConstr(cMaxVar >= c[i], "cMaxContraint");
                }

                // Constraints
                for (int j = 0; j < nJobs; ++j)
                {
                    GRBLinExpr exp3 = 0.0;

                    for (int i = 0; i < nMachines; ++i)
                    {
                        for (int k = 0; k < nJobs; ++k)
                        {
                            exp3.AddTerm(1.0, x[i, j, k]);
                        }
                    }
                    model.AddConstr(exp3 == 1.0, "c1");
                }

                for (int i = 0; i < nMachines; ++i)
                {
                    for (int k = 0; k < nJobs; ++k)
                    {
                        GRBLinExpr exp4 = 0.0;
                        for (int j = 0; j < nJobs; ++j)
                        {
                            exp4.AddTerm(1.0, x[i, j, k]);
                        }
                        model.AddConstr(exp4 <= 1.0, "c2");
                    }
                }

                // Solve
                model.Optimize();

                
                for (int i = 0; i < nMachines; ++i)
                {
                    double totalMachineTime = 0.0;
                    for (int j = 0; j < nJobs; ++j)
                    {
                        for (int k = 0; k < nJobs; ++k)
                        {
                            if (x[i, j, k].X == 1.0)
                            {
                                totalMachineTime += processingTime[i, j];
                                Console.WriteLine($"({i},{j},{k})" + "ProcessingTime: " + processingTime[i, j]);
                            }
                        }
                    }
                    Console.WriteLine(totalMachineTime);
                }

                Console.WriteLine("Obj: " + model.ObjVal);

                // Dispose of model and env
                model.Dispose();
                env.Dispose();

            }
            catch (GRBException e)
            {
                Console.WriteLine("Error code: " + e.ErrorCode + ". " + e.Message);
            }
        }
    }
}
