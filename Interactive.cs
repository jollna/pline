using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Interop;
using Autodesk.AutoCAD.Runtime;
using DotNetARX;
using Microsoft.Win32;
using pline;

namespace NetSelection
{
    public class AcadNetApp : Autodesk.AutoCAD.Runtime.IExtensionApplication
    {
        //重写初始化
        public void Initialize()
        {
            //加载后初始化的程序放在这里 这样程序一加载DLL文件就会执行
            Document doc = Application.DocumentManager.MdiActiveDocument;
            doc.Editor.WriteMessage("\nInitializing......do something useful.");
            //在此处加载 菜单栏程序
            Addmenu();
        }

        //重写结束
        public void Terminate()
        {
            // do somehing to cleanup resource
        }

        //加载菜单栏
        //private Document acDoc;  //获取当前的活动文档 
        //private Editor acEd;
        public void Addmenu()
        {
            
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Editor acEd = acDoc.Editor;
            //Mymainclass mycad = new Mymainclass();
            acEd.WriteMessage("添加菜单栏\n");

            Document doc = Application.DocumentManager.MdiActiveDocument;
            AcadApplication acadApp = Application.AcadApplication as AcadApplication;
            AcadPopupMenu PTaddmenu = null;  //新建菜单栏的对象
            // 创建菜单
            if (PTaddmenu == null)
            {
                PTaddmenu = acadApp.MenuGroups.Item(0).Menus.Add("多段线取整");
                PTaddmenu.AddMenuItem(PTaddmenu.Count, "设置取整量", "SetSumnumber ");
            }

            // 菜单是否显示  看看已经显示的菜单栏里面有没有这一栏
            bool isShowed = false;  //初始化没有显示
            foreach (AcadPopupMenu menu in acadApp.MenuBar)  //遍历现有所有菜单栏
            {
                if (menu == PTaddmenu)
                {
                    isShowed = true;
                    break;
                }
            }

            // 显示菜单 加载自定义的菜单栏
            if (!isShowed)
            {
                PTaddmenu.InsertInMenuBar(acadApp.MenuBar.Count);
            }

        }
    }
    public class Interactive
    {
        [CommandMethod("Zpl")]
        public void AddPoly()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            double width = 0; //初始化线宽
            short colorIndex = 0; //初始化颜色索引值
            int SumLine;
            int index = 2; //初始化多段线顶点数
            ObjectId polyEntId = ObjectId.Null; //声明多段线的ObjectId
            //定义第一个点的用户交互类
            PromptPointOptions optPoint = new PromptPointOptions("\n请输入第一个点<100,200>")
            {
                AllowNone = true //允许用户回车响应
            };
            //返回点的用户提示类
            PromptPointResult resPoint = ed.GetPoint(optPoint);
            //用户按下ESC键，退出
            if (resPoint.Status == PromptStatus.Cancel)
                return;
            Point3d ptStart; //声明第一个输入点
            //用户按回车键
            if (resPoint.Status == PromptStatus.None)
                //得到第一个输入点的默认值
                ptStart = new Point3d(100, 200, 0);
            else
                //得到第一个输入点
                ptStart = resPoint.Value;
            Point3d ptPrevious = ptStart;//保存当前点
            //定义输入下一点的点交互类
            PromptPointOptions optPtKey = new PromptPointOptions("\n请输入下一个点或[线宽(W)/颜色(Y)/闭合(C)/完成(O)]<O>");
            //为点交互类添加关键字
            optPtKey.Keywords.Add("W");
            optPtKey.Keywords.Add("C");
            optPtKey.Keywords.Add("Y");
            optPtKey.Keywords.Add("O");
            optPtKey.Keywords.Default = "O"; //设置默认的关键字
            optPtKey.UseBasePoint = true; //允许使用基准点
            optPtKey.BasePoint = ptPrevious;//设置基准点
            optPtKey.AppendKeywordsToMessage = false;//不将关键字列表添加到提示信息中
            //提示用户输入点
            PromptPointResult resKey = ed.GetPoint(optPtKey);
            //如果用户输入点或关键字，则一直循环
            while (resKey.Status == PromptStatus.OK || resKey.Status == PromptStatus.Keyword)
            {
                Point3d ptNexts ;
                Point3d ptNext = new Point3d();//声明下一个输入点
                //如果用户输入的是关键字集合对象中的关键字
                if (resKey.Status == PromptStatus.Keyword)
                {
                    switch (resKey.StringResult)
                    {
                        case "W":
                            width = GetWidth();
                            break;
                        case "C":
                            //colorIndex = GetColorIndex();
                            using (Transaction trans = db.TransactionManager.StartTransaction())
                            {
                                //打开多段线的状态为写
                                Polyline polyEnt = trans.GetObject(polyEntId, OpenMode.ForWrite) as Polyline;
                                if (polyEnt != null)
                                {
                                    polyEnt.Closed = true;
                                }
                                trans.Commit();
                            }
                            return;
                        case "O":
                            return;
                        case "Y":
                            colorIndex = GetColorIndex();
                            break;
                        default:
                            ed.WriteMessage("\n输入了无效关键字");
                            break;
                    }
                }
                else
                {
                    object tLong = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Autodesk\\AutoCAD\\R19.1\\ACAD-D001:804\\Applications\\RoundPline", "SumLine", 0);
                    SumLine = int.Parse((string)tLong);
                    ptNexts = resKey.Value;//得到户输入的下一点
                    ptNext = GetNewPoint(ptPrevious, ptNexts, SumLine);

                    if (index == 2) //新建多段线
                    {
                        //提取三维点的X、Y坐标值，转化为二维点
                        Point2d pt1 = new Point2d(ptPrevious[0],ptPrevious[1]);
                        Point2d pt2 = new Point2d(ptNext[0], ptNext[1]);
                        Polyline polyEnt = new Polyline();//新建一条多段线
                        //给多段线添加顶点，设置线宽
                        polyEnt.AddVertexAt(0, pt1, 0, width, width);
                        polyEnt.AddVertexAt(1, pt2, 0, width, width);
                        //设置多段线的颜色
                        polyEnt.Color = Color.FromColorIndex(ColorMethod.ByColor, colorIndex);
                        //将多段线添加到图形数据库并返回一个ObjectId(在绘图窗口动态显示多段线)
                        polyEntId = db.AddToModelSpace(polyEnt);
                    }
                    else  //修改多段线，添加最后一个顶点
                    {
                        using (Transaction trans = db.TransactionManager.StartTransaction())
                        {
                            //打开多段线的状态为写
                            Polyline polyEnt = trans.GetObject(polyEntId, OpenMode.ForWrite) as Polyline;
                            if (polyEnt != null)
                            {
                                //继续添加多段线的顶点
                                Point2d ptCurrent = new Point2d(ptNext[0], ptNext[1]);
                                polyEnt.AddVertexAt(index - 1, ptCurrent, 0, width, width);
                                //重新设置多段线的颜色和线宽
                                polyEnt.Color = Color.FromColorIndex(ColorMethod.ByColor, colorIndex);
                                polyEnt.ConstantWidth = width;
                            }
                            trans.Commit();
                        }
                    }
                    index++;
                }
                ptPrevious = ptNext;
                optPtKey.BasePoint = ptPrevious;//重新设置基准点
                resKey = ed.GetPoint(optPtKey); //提示用户输入新的顶点
            }
        }

        //获取增量值
        //public int GetSumLine()
        //{
        //    Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
        //    //定义一个整数的用户交互类
        //    PromptIntegerOptions optInt = new PromptIntegerOptions("\n请输入增量值，默认为1")
        //    {
        //        DefaultValue = 1 //设置默认值
        //    };
        //    //返回一个整数提示类
        //    PromptIntegerResult resInt = ed.GetInteger(optInt);
        //    if (resInt.Status == PromptStatus.OK)
        //    {
        //        //得到用户输入的增量值
        //        int SumLine = resInt.Value;
        //        return SumLine;
        //    }
        //    else
        //        return 1;
        //}

        // 得到用户输入线宽的函数.
        public double GetWidth()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            //定义一个实数的用户交互类.
            PromptDoubleOptions optDou = new PromptDoubleOptions("\n请输入线宽")
            {
                AllowNegative = false, //不允许输入负数
                DefaultValue = 0 //设置默认值
            };
            PromptDoubleResult resDou = ed.GetDouble(optDou);
            if (resDou.Status == PromptStatus.OK)
            {
                Double width = resDou.Value;
                return width; //得到用户输入的线宽
            }
            else
                return 0;
        }

        // 得到用户输入颜色索引值的函数.
        public short GetColorIndex()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            //定义一个整数的用户交互类
            PromptIntegerOptions optInt = new PromptIntegerOptions("\n请输入颜色索引值(0～256)")
            {
                DefaultValue = 0 //设置默认值
            };
            //返回一个整数提示类
            PromptIntegerResult resInt = ed.GetInteger(optInt);
            if (resInt.Status == PromptStatus.OK)
            {
                //得到用户输入的颜色索引值
                short colorIndex = (short)resInt.Value;
                if (colorIndex > 256 | colorIndex < 0)
                    return 0;
                else
                    return colorIndex;
            }
            else
                return 0;
        }
        //得到增量值
        public int GetCoordinate(string ValPrevious, string ValNext,  int SumLine) 
        {
            double length = (double.Parse(ValNext) - double.Parse(ValPrevious)) / SumLine;
            double Roundlength = Round(length, 0);
            int p = (int)(int.Parse(ValPrevious) + Roundlength * SumLine);
            return p;
        }

        public double Round(double d, int i)
        {
            if (d >= 0)
            {
                d += 5 * Math.Pow(10, -(i + 1));
            }
            else
            {
                d += -5 * Math.Pow(10, -(i + 1));
            }
            string str = d.ToString();
            string[] strs = str.Split('.');
            int idot = str.IndexOf('.');
            if (strs.Length == 2)
            {
                string prestr = strs[0];
                string poststr = strs[1];
                if (poststr.Length > i)
                {
                    poststr = str.Substring(idot + 1, i);
                }
                string strd = prestr + "." + poststr;
                d = Double.Parse(strd);
            }
            else
#pragma warning disable CS1717 // 对同一变量进行了赋值
                d = d;
#pragma warning restore CS1717 // 对同一变量进行了赋值
            return d;
        }

        //获取坐标点数据，输出新坐标点数据，保证两点之间的距离为整数
        public Point3d GetNewPoint(Point3d ptPrevious, Point3d ptNext, int SumLine) 
        {
            double numDouble1X = 0;
            double numDouble1Y = 0;

            string[] PreviousSmallX = ptPrevious[0].ToString().Split('.');
            string[] NextIntX = ptNext[0].ToString().Split('.');
            string[] PreviousSmallY = ptPrevious[1].ToString().Split('.');
            string[] NextIntY = ptNext[1].ToString().Split('.');

            if (PreviousSmallY.Length == 2 && PreviousSmallX.Length == 2)
            {
                string decimalX = GetQuadrant(ptPrevious[0], ptNext[0]);
                string decimalY = GetQuadrant(ptPrevious[1], ptNext[1]);
                int sumX = GetCoordinate(PreviousSmallX[0], NextIntX[0], SumLine);
                int sumY = GetCoordinate(PreviousSmallY[0], NextIntY[0], SumLine);
                string X = sumX + "." + decimalX;
                string Y = sumY + "." + decimalY;
                numDouble1X = double.Parse(X);
                numDouble1Y = double.Parse(Y);
            }
            else if (PreviousSmallY.Length == 1 && PreviousSmallX.Length == 1)
            {
                int sumY = GetCoordinate(PreviousSmallY[0], NextIntY[0], SumLine);
                int sumX = GetCoordinate(PreviousSmallX[0], NextIntX[0], SumLine);
                string Y = sumY.ToString();
                string X = sumX.ToString();
                numDouble1Y = double.Parse(Y);
                numDouble1X = double.Parse(X);
            }
            else if (PreviousSmallY.Length == 1)
            {
                string decimalX = GetQuadrant(ptPrevious[0], ptNext[0]);
                int sumY = GetCoordinate(PreviousSmallY[0], NextIntY[0], SumLine);
                int sumX = GetCoordinate(PreviousSmallX[0], NextIntX[0], SumLine);
                string Y = sumY.ToString();
                string X = sumX + "." + decimalX;
                numDouble1Y = double.Parse(Y);
                numDouble1X = double.Parse(X);
            }
            else if (PreviousSmallX.Length == 1)
            {
                string decimalY = GetQuadrant(ptPrevious[1], ptNext[1]);
                int sumY = GetCoordinate(PreviousSmallY[0], NextIntY[0], SumLine);
                int sumX = GetCoordinate(PreviousSmallX[0], NextIntX[0], SumLine);
                string Y = sumY + "." + decimalY;
                string X = sumX.ToString();
                numDouble1Y = double.Parse(Y);
                numDouble1X = double.Parse(X);
            }
            ptNext = new Point3d(numDouble1X, numDouble1Y, 0);
            return ptNext;
        }

        //根据象限判断坐标点数值，保证坐标跨象限也为整数
        public string GetQuadrant(double ptPrevious, double ptNext)
        {
            if ((ptPrevious > 0 && ptNext > 0) || (ptPrevious < 0 && ptNext < 0))
            {
                string[] PreviousSmall = ptPrevious.ToString("F8").Split('.');
                string X = PreviousSmall[1];
                return X;
            }else
            {
                string[] PreviousSmall = ptPrevious.ToString("F8").Split('.');
                string Y = "0." + PreviousSmall[1];
                double numDouble1Y = double.Parse(Y);
                double X = 1- numDouble1Y;
                string[] z = X.ToString().Split('.');
                return z[1];
            }
        }

        [CommandMethod("SetSumnumber")]
        public void SetSumnumber()
        {
            Form1 myfrom = new Form1();
            Autodesk.AutoCAD.ApplicationServices.Application.ShowModelessDialog(myfrom); //在CAD里头显示界面  非模态显示
        }
    }
}
