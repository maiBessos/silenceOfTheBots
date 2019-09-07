using GoE.GameLogic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Graph = System.Windows.Forms.DataVisualization.Charting;
using Microsoft.CSharp;
using System.CodeDom;
using System.Reflection;
using AFunctionTreeNode = GoE.Utils.Algorithms.FunctionTreeNode.AFunctionTreeNode;
using RootFuncTreeNode = GoE.Utils.Algorithms.FunctionTreeNode.RootFuncTreeNode;
using GoE.Utils.DynamicCompilation.SandboxCode;
using GoE.Utils.Extensions;

using DynamicCompilation;



namespace GoE.Utils
{
    public partial class frmUtils : Form
    {
        public frmUtils()
        {
            InitializeComponent();
        }

        private System.Windows.Forms.TabControl tabListUtilsMenu;
        private System.Windows.Forms.TabPage tabResultGraphs;
        private System.Windows.Forms.Button grphBtnRenameSeries;
        private System.Windows.Forms.Button grphBtnClearAllSeries;
        private System.Windows.Forms.ListBox grphLstChartFiles;
        private System.Windows.Forms.Button grphBtnAddChartFile;
        private System.Windows.Forms.Button grphBtnGenerateChart;

        Dictionary<string, int> prevInput = null;
        List<List<Dictionary<string, string>>> chartTables = new List<List<Dictionary<string, string>>>();
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox lstY;
        private System.Windows.Forms.ListBox lstX;
        private System.Windows.Forms.TabPage tabDispExpressions;
        private Button btnDisplaySandbox;
        private Button btnChooseSandbox;
        private Button btnOpenExpressions;
        private HScrollBar hscrlExp;
        private VScrollBar vscrlExp;
        private Button btnClipboardSeries;
        private PictureBox pictureChartOutPreview;
        private TabPage tabSql;
        private CheckBox chkBackupCSV;
        private Button btnTranslateCSV;
        private TextBox txtFirstRowFieldNames;
        private TabPage tabPage1;
        private Button button1;
        private UI.SizeablePanel sizeablePanel1;
        private Button btnMrg;
        List<string> chartFileCaptions = new System.Collections.Generic.List<string>();
 
        private string getWordValue(string word, bool isXAxisPsi)
        {
            if (word.Contains('('))
            {
                // this file is theoretical result, which tells both psi and r_p
                if (isXAxisPsi)
                    word = word.Substring(0, word.IndexOf('(')); // take the first part of the word, which is psi
                else
                {
                    word = word.Substring(word.IndexOf('(') + 1); // extract the text in parenthesis
                    word = word.Substring(0, word.IndexOf(')'));
                }
            }
            return word;
        }

        private Bitmap makePointsChart(
            int chartWidth, int chartHeight,
            List<Tuple<string, List<PointF>>> seriesList)
        {
            List<string> allLines = new System.Collections.Generic.List<string>();

            int pointsCount = 0;

            bool posInfinityY = false;
            double minX = double.MaxValue, maxX = double.MinValue, minY = double.MaxValue, maxY = double.MinValue;
            foreach (var series in seriesList)
            {

                foreach (PointF p in series.Item2)
                {

                    if (p.Y == double.PositiveInfinity)
                        posInfinityY = true;
                    else
                        maxY = Math.Max(maxY, p.Y);
                    minX = Math.Min(minX, p.X);
                    minY = Math.Min(minY, p.Y);
                    maxX = Math.Max(maxX, p.X);

                }
                pointsCount = Math.Max(pointsCount, series.Item2.Count);
            }
            if (posInfinityY)
                maxY *= 2;

            Graph.Chart chart = new Graph.Chart();
            chart.Location = new System.Drawing.Point(10, 10);
            chart.Size = new System.Drawing.Size(700, 700);

            // Add a chartarea called "draw", add axes to it and color the area black
            chart.ChartAreas.Add("draw");
            chart.ChartAreas["draw"].AxisX.Title = "X";
            chart.ChartAreas["draw"].AxisY.Title = "Y";
            chart.ChartAreas["draw"].AxisX.Minimum = minX;
            chart.ChartAreas["draw"].AxisX.Maximum = maxX;
            chart.ChartAreas["draw"].AxisX.Interval = Math.Min((maxX - minX) / pointsCount, 5);
            chart.ChartAreas["draw"].AxisX.MajorGrid.LineColor = Color.Black;
            chart.ChartAreas["draw"].AxisX.MajorGrid.LineDashStyle = Graph.ChartDashStyle.Dash;
            chart.ChartAreas["draw"].AxisY.Minimum = 0;
            chart.ChartAreas["draw"].AxisY.Maximum = maxY;
            chart.ChartAreas["draw"].AxisY.Interval = maxY / 10;
            chart.ChartAreas["draw"].AxisY.MajorGrid.LineColor = Color.Black;
            chart.ChartAreas["draw"].AxisY.MajorGrid.LineDashStyle = Graph.ChartDashStyle.Dash;

            chart.ChartAreas["draw"].BackColor = Color.White;

            Color[] colors =
                new Color[]{Color.Blue,Color.Magenta, Color.Orange, Color.DarkSlateBlue, Color.Green,
                              Color.Brown,Color.Yellow,Color.Gold,Color.Goldenrod, Color.Red, Color.Black, Color.DarkGreen, Color.Pink};

            int tableIdx = 0;
            foreach (var series in seriesList)
            {

                chart.Series.Add(series.Item1);
                chart.Series[series.Item1].ChartType = Graph.SeriesChartType.Line;
                chart.Series[series.Item1].Color = colors[tableIdx];
                chart.Series[series.Item1].BorderWidth = 3;

                chart.Series[series.Item1].LegendText = series.Item1;


                List<PointF> points = new System.Collections.Generic.List<System.Drawing.PointF>();
                foreach (PointF lp in series.Item2)
                {
                    PointF p = lp;
                    if (p.Y == double.PositiveInfinity)
                        p.Y = (float)maxY;
                    points.Add(p);
                }
                points.Sort(new Comparison<PointF>((p1, p2) => p1.X.CompareTo(p2.X)));
                for (int pointI = 0; pointI < points.Count; ++pointI)
                    chart.Series[series.Item1].Points.AddXY(points[pointI].X, points[pointI].Y);


                ++tableIdx;
            }

            chart.Legends.Add(new Legend("Legend"));
            chart.Legends["Legend"].BorderColor = Color.Tomato;
            Bitmap res = new System.Drawing.Bitmap(chartWidth, chartHeight);
            chart.DrawToBitmap(res, new System.Drawing.Rectangle(0, 0, chartWidth, chartHeight));
            return res;
        }

        /// <summary>
        /// translates each function into a list of points, then calls makePointsChart()
        /// </summary>
        private Bitmap makeFuncChart(
            int chartWidth, int chartHeight,
            Dictionary<string,AFunctionTreeNode> funcs, double minParam, double maxParam, double sampleDiff = 0)
        {
            List<Tuple<string, List<PointF>>> seriesList = new System.Collections.Generic.List<System.Tuple<string, System.Collections.Generic.List<System.Drawing.PointF>>>();
            if (sampleDiff == 0)
                sampleDiff = (maxParam - minParam) / 50;
            foreach (var s in funcs)
                seriesList.Add(Tuple.Create(s.Key, s.Value.sample(minParam, maxParam, sampleDiff)));

            return makePointsChart(chartWidth, chartHeight, seriesList);
        }

        List<double> extractValueDifference(List<string> values)
        {
            // TODO:if the cahnging value is a number, and only part of the number changes i.e. 31,32,33,34 -  we want to parse the values 31,32,33,34 and not just 1,2,3,4

            List<string> valuesToEdit = new List<string>(values);
            string sharedPrefix = "", sharedSuffix = "";
            int strLen = 0;
            bool checkNext = true;
            // find longest prefix shared by all values
            while (checkNext)
            {
                ++strLen;
                sharedPrefix = valuesToEdit.First().Substring(0, strLen);
                foreach (var v in valuesToEdit)
                    if(!v.StartsWith(sharedPrefix))
                    {
                        checkNext = false;
                        break;
                    }
               
            }
            if (strLen == 1)
                sharedPrefix = "";
            else
                sharedPrefix = valuesToEdit.First().Substring(0, strLen-1);

            for (int vi = 0; vi < valuesToEdit.Count; ++vi)
                valuesToEdit[vi] = valuesToEdit[vi].Substring(sharedPrefix.Count());

            strLen = 0;
            checkNext = true;
            // find longest suffix shared by all values
            while (checkNext)
            {
                ++strLen;
                sharedSuffix = valuesToEdit.First().Substring(valuesToEdit.First().Count()-strLen, strLen);
                foreach (var v in valuesToEdit)
                    if (!v.EndsWith(sharedSuffix))
                    {
                        checkNext = false;
                        break;
                    }
            }
            if (strLen == 1)
                sharedSuffix = "";
            else
                sharedSuffix = valuesToEdit.First().Substring(valuesToEdit.First().Count() - (strLen-1), strLen-1);


            List<double> res = new List<double>();
            for (int vi = 0; vi < valuesToEdit.Count; ++vi)
            {
                    valuesToEdit[vi] = valuesToEdit[vi].Substring(0, valuesToEdit[vi].Count() - sharedSuffix.Count());
                    res.Add(double.Parse(valuesToEdit[vi]));
              
            }
            return res;

            
        }
        List<string> getValues(List<Dictionary<string,string>> table, string column)
        {
            List<string> res = new List<string>();
            foreach (var v in table)
                res.Add(v[column]);
            return res;
        }
        private Bitmap makeChart(List<List<Dictionary<string, string>>> tables, List<string> captions, string outFilename, string xColumn, string yColumn, string xColumnLabel, string yColumnLabel)
        {

            List<string> allLines = new System.Collections.Generic.List<string>();
            int pointsCount = 0;

            bool posInfinityY = false;
            double minX = double.MaxValue, maxX = double.MinValue, minY = double.MaxValue, maxY = double.MinValue;
            foreach (var table in tables)
            {

                List<double> xVals = extractValueDifference(getValues(table, xColumn));
                List<double> yVals = extractValueDifference(getValues(table, yColumn));
                for(int vi = 0; vi < xVals.Count; ++vi)
                //foreach (Dictionary<string, string> tableLine in table)
                {
                    double xVal = xVals[vi];//double.Parse(tableLine[xColumn]);
                    double yVal = yVals[vi];//double.Parse(tableLine[yColumn]);

                    if (yVal == double.PositiveInfinity)
                        posInfinityY = true;
                    else
                        maxY = Math.Max(maxY, yVal);
                    minX = Math.Min(minX, xVal);
                    minY = Math.Min(minY, yVal);
                    maxX = Math.Max(maxX, xVal);

                }
                pointsCount = Math.Max(pointsCount, table.Count);
            }
            if (posInfinityY)
                maxY *= 2;
            if (minY == maxY)
                minY = 0;

            Graph.Chart chart = new Graph.Chart();
            chart.Location = new System.Drawing.Point(10, 10);
            chart.Size = new System.Drawing.Size(700, 700);
            // Add a chartarea called "draw", add axes to it and color the area black
            chart.ChartAreas.Add("draw");
            chart.ChartAreas["draw"].AxisY.Enabled = AxisEnabled.True;
            chart.ChartAreas["draw"].AxisY2.Enabled =  AxisEnabled.False;
            chart.ChartAreas["draw"].AxisX.Title = xColumnLabel;
            //if (isXAxisPsi)
            //    chart.ChartAreas["draw"].AxisX.Title = "Pursuers count";
            //else
            //    chart.ChartAreas["draw"].AxisX.Title = "Pursuers' vel.";

            if (yColumnLabel == "leaked")
                yColumnLabel = "Avg. Leaked per eve.";
            chart.ChartAreas["draw"].AxisY2.Title = yColumnLabel;
            //chart.ChartAreas["draw"].AxisX.Crossing = maxY;
            


            //chart.ChartAreas["draw"].AxisX.Minimum = minPsi;
            //chart.ChartAreas["draw"].AxisX.Maximum = maxPsi;
            chart.ChartAreas["draw"].AxisX.IsReversed = true;
            chart.ChartAreas["draw"].AxisX.Minimum = minX;
            chart.ChartAreas["draw"].AxisX.Maximum = maxX;
            chart.ChartAreas["draw"].AxisX.Interval = (maxX - minX) / 4; //Math.Min((maxX - minX) / pointsCount, 5);
            chart.ChartAreas["draw"].AxisX.MajorGrid.LineColor = Color.Black;
            chart.ChartAreas["draw"].AxisX.MajorGrid.LineDashStyle = Graph.ChartDashStyle.Dash;
            chart.ChartAreas["draw"].AxisY2.Minimum = 0;
            //chart.ChartAreas["draw"].AxisY2.Maximum = maxLeakedData;
            //chart.ChartAreas["draw"].AxisY2.Interval = Math.Min(50,maxLeakedData/2);
            chart.ChartAreas["draw"].AxisY2.Maximum = maxY + 0.001;
            chart.ChartAreas["draw"].AxisY2.Interval = (maxY-minY) / 4; //maxY / 10;
            chart.ChartAreas["draw"].AxisY2.MajorGrid.LineColor = Color.Black;
            chart.ChartAreas["draw"].AxisY2.MajorGrid.LineDashStyle = Graph.ChartDashStyle.Dash;
            chart.ChartAreas["draw"].BackColor = Color.White;

            Color[] colors =
                new Color[]{Color.Blue,Color.Magenta, Color.Orange, Color.DarkSlateBlue, Color.Green,
                              Color.Brown,Color.Yellow,Color.Gold,Color.Goldenrod, Color.Red, Color.Black, Color.DarkGreen, Color.Pink};

            for(int tableIdx = 0; tableIdx < captions.Count; ++tableIdx)
            {
                //lines = File.ReadAllLines(file);
                //firstLine = true;

                chart.Series.Add(captions[tableIdx]);
                chart.Series[captions[tableIdx]].ChartType = Graph.SeriesChartType.Line;
                chart.Series[captions[tableIdx]].Color = colors[tableIdx];
                chart.Series[captions[tableIdx]].BorderWidth = 3;
                chart.Series[captions[tableIdx]].MarkerStyle = MarkerStyle.Circle;
                chart.Series[captions[tableIdx]].MarkerColor = colors[tableIdx];

                //if (prevPsi == -1)
                //    chart.Series[psi.ToString()].LegendText = "pursuers count: " + psi.ToString();
                //else
                chart.Series[captions[tableIdx]].LegendText = captions[tableIdx];


                //foreach (string l in lines)
                //{
                //    if (firstLine)
                //    {
                //        firstLine = false;
                //        continue;
                //    }
                //    string[] words = l.Split(new char[1] { '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
                //    for (int wi = 0; wi < words.Count(); ++wi)
                //    {
                //        words[wi] = words[wi].Replace("\t", "");
                //        words[wi] = words[wi].Replace(",", "");
                //    }

                //    words[0] = getWordValue(words[0], isXAxisPsi);
                //    int psi = (int)double.Parse(words[0]);
                //    double leakedData;
                //    if (!double.TryParse(words[1], out leakedData) || double.IsInfinity(leakedData))
                //        leakedData = maxLeakedData;

                //    chart.Series[captions[functionChanges]].Points.AddXY(psi, (double)leakedData);
                //}

                List<PointF> points = new System.Collections.Generic.List<System.Drawing.PointF>();



                //foreach (Dictionary<string, string> tableLine in tables[tableIdx])
                //{
                //    double xVal = double.Parse(tableLine[xColumn]);
                //    double yVal = double.Parse(tableLine[yColumn]);
                List<double> xVals = extractValueDifference(getValues(tables[tableIdx], xColumn));
                List<double> yVals = extractValueDifference(getValues(tables[tableIdx], yColumn));
                for (int vi = 0; vi < xVals.Count; ++vi)
                {
                    double xVal = xVals[vi];
                    double yVal = yVals[vi];
                    if (yVal == double.PositiveInfinity)
                        yVal = maxY;
                    points.Add(new PointF((float)xVal, (float)yVal));
                }
                points.Sort(new Comparison<PointF>((p1, p2) => p1.X.CompareTo(p2.X)));
                for (int pointI = 0; pointI < points.Count; ++pointI)
                    chart.Series[captions[tableIdx]].Points.AddXY(points[pointI].X, points[pointI].Y);
                
            }

            chart.Legends.Add(new Legend("Legend"));
            chart.Legends["Legend"].BorderColor = Color.Tomato;

            if(outFilename != null)
                chart.SaveImage(outFilename, ChartImageFormat.Png);

            Bitmap res = new System.Drawing.Bitmap(700, 700);

            chart.Legends["Legend"].Position.Auto = false;
            chart.Legends["Legend"].Position = new ElementPosition(80, 0, 25, 10);
            chart.Legends["Legend"].IsDockedInsideChartArea = false;
            chart.DrawToBitmap(res, new System.Drawing.Rectangle(0, 0, 700, 700));
            return res;
        }
        private Bitmap makeChart(List<string> files, List<string> captions, string outFilename, string xColumn, string yColumn, string xColumnLabel, string yColumnLabel)
        {
            List<List<Dictionary<string, string>>> tables = new System.Collections.Generic.List<System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, string>>>();
            
            foreach (string s in files)
            {
                try
                {
                    tables.Add(FileUtils.readTable(s));
                }
                catch(Exception ex)
                {
                    MessageBox.Show("couldn't load table:" + s + "." + ex.Message);
                }
            }

            return makeChart(tables,captions, outFilename, xColumn,yColumn,xColumnLabel,yColumnLabel);
        }

        private void InitializeComponent()
        {
            this.tabListUtilsMenu = new System.Windows.Forms.TabControl();
            this.tabResultGraphs = new System.Windows.Forms.TabPage();
            this.btnMrg = new System.Windows.Forms.Button();
            this.btnClipboardSeries = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.lstY = new System.Windows.Forms.ListBox();
            this.lstX = new System.Windows.Forms.ListBox();
            this.grphBtnRenameSeries = new System.Windows.Forms.Button();
            this.grphBtnClearAllSeries = new System.Windows.Forms.Button();
            this.grphLstChartFiles = new System.Windows.Forms.ListBox();
            this.grphBtnAddChartFile = new System.Windows.Forms.Button();
            this.grphBtnGenerateChart = new System.Windows.Forms.Button();
            this.pictureChartOutPreview = new System.Windows.Forms.PictureBox();
            this.tabDispExpressions = new System.Windows.Forms.TabPage();
            this.hscrlExp = new System.Windows.Forms.HScrollBar();
            this.vscrlExp = new System.Windows.Forms.VScrollBar();
            this.btnDisplaySandbox = new System.Windows.Forms.Button();
            this.btnChooseSandbox = new System.Windows.Forms.Button();
            this.btnOpenExpressions = new System.Windows.Forms.Button();
            this.tabSql = new System.Windows.Forms.TabPage();
            this.txtFirstRowFieldNames = new System.Windows.Forms.TextBox();
            this.chkBackupCSV = new System.Windows.Forms.CheckBox();
            this.btnTranslateCSV = new System.Windows.Forms.Button();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.sizeablePanel1 = new GoE.UI.SizeablePanel();
            this.button1 = new System.Windows.Forms.Button();
            this.tabListUtilsMenu.SuspendLayout();
            this.tabResultGraphs.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureChartOutPreview)).BeginInit();
            this.tabDispExpressions.SuspendLayout();
            this.tabSql.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabListUtilsMenu
            // 
            this.tabListUtilsMenu.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabListUtilsMenu.Controls.Add(this.tabResultGraphs);
            this.tabListUtilsMenu.Controls.Add(this.tabDispExpressions);
            this.tabListUtilsMenu.Controls.Add(this.tabSql);
            this.tabListUtilsMenu.Controls.Add(this.tabPage1);
            this.tabListUtilsMenu.Location = new System.Drawing.Point(0, 0);
            this.tabListUtilsMenu.Name = "tabListUtilsMenu";
            this.tabListUtilsMenu.SelectedIndex = 0;
            this.tabListUtilsMenu.Size = new System.Drawing.Size(931, 532);
            this.tabListUtilsMenu.TabIndex = 4;
            // 
            // tabResultGraphs
            // 
            this.tabResultGraphs.Controls.Add(this.btnMrg);
            this.tabResultGraphs.Controls.Add(this.btnClipboardSeries);
            this.tabResultGraphs.Controls.Add(this.label2);
            this.tabResultGraphs.Controls.Add(this.label1);
            this.tabResultGraphs.Controls.Add(this.lstY);
            this.tabResultGraphs.Controls.Add(this.lstX);
            this.tabResultGraphs.Controls.Add(this.grphBtnRenameSeries);
            this.tabResultGraphs.Controls.Add(this.grphBtnClearAllSeries);
            this.tabResultGraphs.Controls.Add(this.grphLstChartFiles);
            this.tabResultGraphs.Controls.Add(this.grphBtnAddChartFile);
            this.tabResultGraphs.Controls.Add(this.grphBtnGenerateChart);
            this.tabResultGraphs.Controls.Add(this.pictureChartOutPreview);
            this.tabResultGraphs.Location = new System.Drawing.Point(4, 22);
            this.tabResultGraphs.Name = "tabResultGraphs";
            this.tabResultGraphs.Padding = new System.Windows.Forms.Padding(3);
            this.tabResultGraphs.Size = new System.Drawing.Size(923, 506);
            this.tabResultGraphs.TabIndex = 0;
            this.tabResultGraphs.Text = "Generate Result Graph";
            this.tabResultGraphs.UseVisualStyleBackColor = true;
            this.tabResultGraphs.Click += new System.EventHandler(this.tabResultGraphs_Click);
            // 
            // btnMrg
            // 
            this.btnMrg.Location = new System.Drawing.Point(278, 44);
            this.btnMrg.Name = "btnMrg";
            this.btnMrg.Size = new System.Drawing.Size(88, 37);
            this.btnMrg.TabIndex = 15;
            this.btnMrg.Text = "Merge vals into series";
            this.btnMrg.UseVisualStyleBackColor = true;
            this.btnMrg.Click += new System.EventHandler(this.btnMrg_Click);
            // 
            // btnClipboardSeries
            // 
            this.btnClipboardSeries.Location = new System.Drawing.Point(372, 3);
            this.btnClipboardSeries.Name = "btnClipboardSeries";
            this.btnClipboardSeries.Size = new System.Drawing.Size(88, 37);
            this.btnClipboardSeries.TabIndex = 13;
            this.btnClipboardSeries.Text = "paste clipboard series";
            this.btnClipboardSeries.UseVisualStyleBackColor = true;
            this.btnClipboardSeries.Click += new System.EventHandler(this.btnClipboardSeries_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(8, 136);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(80, 13);
            this.label2.TabIndex = 12;
            this.label2.Text = "Select X Value:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 322);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 13);
            this.label1.TabIndex = 11;
            this.label1.Text = "Select Y Value:";
            // 
            // lstY
            // 
            this.lstY.FormattingEnabled = true;
            this.lstY.Location = new System.Drawing.Point(10, 338);
            this.lstY.Name = "lstY";
            this.lstY.Size = new System.Drawing.Size(471, 160);
            this.lstY.TabIndex = 10;
            this.lstY.SelectedIndexChanged += new System.EventHandler(this.lstY_SelectedIndexChanged);
            // 
            // lstX
            // 
            this.lstX.FormattingEnabled = true;
            this.lstX.Location = new System.Drawing.Point(10, 159);
            this.lstX.Name = "lstX";
            this.lstX.Size = new System.Drawing.Size(471, 160);
            this.lstX.TabIndex = 9;
            this.lstX.SelectedIndexChanged += new System.EventHandler(this.lstX_SelectedIndexChanged);
            // 
            // grphBtnRenameSeries
            // 
            this.grphBtnRenameSeries.Location = new System.Drawing.Point(372, 46);
            this.grphBtnRenameSeries.Name = "grphBtnRenameSeries";
            this.grphBtnRenameSeries.Size = new System.Drawing.Size(88, 37);
            this.grphBtnRenameSeries.TabIndex = 8;
            this.grphBtnRenameSeries.Text = "Rename series";
            this.grphBtnRenameSeries.UseVisualStyleBackColor = true;
            this.grphBtnRenameSeries.Click += new System.EventHandler(this.grphBtnRenameSeries_Click);
            // 
            // grphBtnClearAllSeries
            // 
            this.grphBtnClearAllSeries.Location = new System.Drawing.Point(278, 89);
            this.grphBtnClearAllSeries.Name = "grphBtnClearAllSeries";
            this.grphBtnClearAllSeries.Size = new System.Drawing.Size(88, 37);
            this.grphBtnClearAllSeries.TabIndex = 7;
            this.grphBtnClearAllSeries.Text = "clear";
            this.grphBtnClearAllSeries.UseVisualStyleBackColor = true;
            this.grphBtnClearAllSeries.Click += new System.EventHandler(this.grphBtnClearAllSeries_Click);
            // 
            // grphLstChartFiles
            // 
            this.grphLstChartFiles.FormattingEnabled = true;
            this.grphLstChartFiles.HorizontalScrollbar = true;
            this.grphLstChartFiles.Location = new System.Drawing.Point(6, 3);
            this.grphLstChartFiles.Name = "grphLstChartFiles";
            this.grphLstChartFiles.Size = new System.Drawing.Size(263, 121);
            this.grphLstChartFiles.TabIndex = 6;
            this.grphLstChartFiles.SelectedIndexChanged += new System.EventHandler(this.grphLstChartFiles_SelectedIndexChanged);
            // 
            // grphBtnAddChartFile
            // 
            this.grphBtnAddChartFile.Location = new System.Drawing.Point(278, 3);
            this.grphBtnAddChartFile.Name = "grphBtnAddChartFile";
            this.grphBtnAddChartFile.Size = new System.Drawing.Size(88, 37);
            this.grphBtnAddChartFile.TabIndex = 5;
            this.grphBtnAddChartFile.Text = "add file series";
            this.grphBtnAddChartFile.UseVisualStyleBackColor = true;
            this.grphBtnAddChartFile.Click += new System.EventHandler(this.grphBtnAddChartFile_Click);
            // 
            // grphBtnGenerateChart
            // 
            this.grphBtnGenerateChart.Location = new System.Drawing.Point(372, 87);
            this.grphBtnGenerateChart.Name = "grphBtnGenerateChart";
            this.grphBtnGenerateChart.Size = new System.Drawing.Size(88, 37);
            this.grphBtnGenerateChart.TabIndex = 4;
            this.grphBtnGenerateChart.Text = "generate chart";
            this.grphBtnGenerateChart.UseVisualStyleBackColor = true;
            this.grphBtnGenerateChart.Click += new System.EventHandler(this.grphBtnGenerateChart_Click);
            // 
            // pictureChartOutPreview
            // 
            this.pictureChartOutPreview.Dock = System.Windows.Forms.DockStyle.Right;
            this.pictureChartOutPreview.Location = new System.Drawing.Point(487, 3);
            this.pictureChartOutPreview.Name = "pictureChartOutPreview";
            this.pictureChartOutPreview.Size = new System.Drawing.Size(433, 500);
            this.pictureChartOutPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureChartOutPreview.TabIndex = 14;
            this.pictureChartOutPreview.TabStop = false;
            // 
            // tabDispExpressions
            // 
            this.tabDispExpressions.Controls.Add(this.hscrlExp);
            this.tabDispExpressions.Controls.Add(this.vscrlExp);
            this.tabDispExpressions.Controls.Add(this.btnDisplaySandbox);
            this.tabDispExpressions.Controls.Add(this.btnChooseSandbox);
            this.tabDispExpressions.Controls.Add(this.btnOpenExpressions);
            this.tabDispExpressions.Location = new System.Drawing.Point(4, 22);
            this.tabDispExpressions.Name = "tabDispExpressions";
            this.tabDispExpressions.Padding = new System.Windows.Forms.Padding(3);
            this.tabDispExpressions.Size = new System.Drawing.Size(923, 506);
            this.tabDispExpressions.TabIndex = 1;
            this.tabDispExpressions.Text = "Expression Displayer";
            this.tabDispExpressions.UseVisualStyleBackColor = true;
            this.tabDispExpressions.Click += new System.EventHandler(this.tabDispExpressions_Click);
            this.tabDispExpressions.Paint += new System.Windows.Forms.PaintEventHandler(this.tabDispExpressions_Paint);
            this.tabDispExpressions.Enter += new System.EventHandler(this.tabDispExpressions_Enter);
            // 
            // hscrlExp
            // 
            this.hscrlExp.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.hscrlExp.Location = new System.Drawing.Point(3, 484);
            this.hscrlExp.Maximum = 1000;
            this.hscrlExp.Name = "hscrlExp";
            this.hscrlExp.Size = new System.Drawing.Size(901, 17);
            this.hscrlExp.TabIndex = 4;
            this.hscrlExp.Scroll += new System.Windows.Forms.ScrollEventHandler(this.hscrlExp_Scroll);
            // 
            // vscrlExp
            // 
            this.vscrlExp.Dock = System.Windows.Forms.DockStyle.Right;
            this.vscrlExp.Location = new System.Drawing.Point(902, 3);
            this.vscrlExp.Maximum = 1000;
            this.vscrlExp.Name = "vscrlExp";
            this.vscrlExp.Size = new System.Drawing.Size(18, 500);
            this.vscrlExp.TabIndex = 3;
            this.vscrlExp.Scroll += new System.Windows.Forms.ScrollEventHandler(this.vscrlExp_Scroll);
            // 
            // btnDisplaySandbox
            // 
            this.btnDisplaySandbox.Location = new System.Drawing.Point(320, 15);
            this.btnDisplaySandbox.Name = "btnDisplaySandbox";
            this.btnDisplaySandbox.Size = new System.Drawing.Size(139, 23);
            this.btnDisplaySandbox.TabIndex = 2;
            this.btnDisplaySandbox.Text = "Display sandbox";
            this.btnDisplaySandbox.UseVisualStyleBackColor = true;
            this.btnDisplaySandbox.Click += new System.EventHandler(this.btnDisplaySandbox_Click);
            // 
            // btnChooseSandbox
            // 
            this.btnChooseSandbox.Location = new System.Drawing.Point(175, 15);
            this.btnChooseSandbox.Name = "btnChooseSandbox";
            this.btnChooseSandbox.Size = new System.Drawing.Size(139, 23);
            this.btnChooseSandbox.TabIndex = 1;
            this.btnChooseSandbox.Text = "Choose Sandbox class";
            this.btnChooseSandbox.UseVisualStyleBackColor = true;
            this.btnChooseSandbox.Click += new System.EventHandler(this.btnChooseSandbox_Click);
            // 
            // btnOpenExpressions
            // 
            this.btnOpenExpressions.Location = new System.Drawing.Point(13, 15);
            this.btnOpenExpressions.Name = "btnOpenExpressions";
            this.btnOpenExpressions.Size = new System.Drawing.Size(139, 23);
            this.btnOpenExpressions.TabIndex = 0;
            this.btnOpenExpressions.Text = "Open Expression Window";
            this.btnOpenExpressions.UseVisualStyleBackColor = true;
            this.btnOpenExpressions.Click += new System.EventHandler(this.btnOpenExpressions_Click);
            // 
            // tabSql
            // 
            this.tabSql.Controls.Add(this.txtFirstRowFieldNames);
            this.tabSql.Controls.Add(this.chkBackupCSV);
            this.tabSql.Controls.Add(this.btnTranslateCSV);
            this.tabSql.Location = new System.Drawing.Point(4, 22);
            this.tabSql.Name = "tabSql";
            this.tabSql.Padding = new System.Windows.Forms.Padding(3);
            this.tabSql.Size = new System.Drawing.Size(923, 506);
            this.tabSql.TabIndex = 2;
            this.tabSql.Text = "Sql";
            this.tabSql.UseVisualStyleBackColor = true;
            // 
            // txtFirstRowFieldNames
            // 
            this.txtFirstRowFieldNames.Location = new System.Drawing.Point(242, 21);
            this.txtFirstRowFieldNames.Name = "txtFirstRowFieldNames";
            this.txtFirstRowFieldNames.Size = new System.Drawing.Size(192, 20);
            this.txtFirstRowFieldNames.TabIndex = 2;
            this.txtFirstRowFieldNames.Text = "First row CSV Field names ";
            // 
            // chkBackupCSV
            // 
            this.chkBackupCSV.AutoSize = true;
            this.chkBackupCSV.Location = new System.Drawing.Point(8, 50);
            this.chkBackupCSV.Name = "chkBackupCSV";
            this.chkBackupCSV.Size = new System.Drawing.Size(119, 17);
            this.chkBackupCSV.TabIndex = 1;
            this.chkBackupCSV.Text = "backup original files";
            this.chkBackupCSV.UseVisualStyleBackColor = true;
            // 
            // btnTranslateCSV
            // 
            this.btnTranslateCSV.Location = new System.Drawing.Point(8, 21);
            this.btnTranslateCSV.Name = "btnTranslateCSV";
            this.btnTranslateCSV.Size = new System.Drawing.Size(214, 23);
            this.btnTranslateCSV.TabIndex = 0;
            this.btnTranslateCSV.Text = "Translate Sqlite CSV file to CSV table";
            this.btnTranslateCSV.UseVisualStyleBackColor = true;
            this.btnTranslateCSV.Click += new System.EventHandler(this.btn_Click);
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.sizeablePanel1);
            this.tabPage1.Controls.Add(this.button1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(923, 506);
            this.tabPage1.TabIndex = 3;
            this.tabPage1.Text = "IntrusionGraph";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // sizeablePanel1
            // 
            this.sizeablePanel1.BackColor = System.Drawing.Color.White;
            this.sizeablePanel1.Location = new System.Drawing.Point(184, 269);
            this.sizeablePanel1.Name = "sizeablePanel1";
            this.sizeablePanel1.Size = new System.Drawing.Size(200, 100);
            this.sizeablePanel1.TabIndex = 1;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(88, 45);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // frmUtils
            // 
            this.ClientSize = new System.Drawing.Size(931, 532);
            this.Controls.Add(this.tabListUtilsMenu);
            this.Name = "frmUtils";
            this.Load += new System.EventHandler(this.frmUtils_Load);
            this.tabListUtilsMenu.ResumeLayout(false);
            this.tabResultGraphs.ResumeLayout(false);
            this.tabResultGraphs.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureChartOutPreview)).EndInit();
            this.tabDispExpressions.ResumeLayout(false);
            this.tabSql.ResumeLayout(false);
            this.tabSql.PerformLayout();
            this.tabPage1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        HashSet<string> commonColumns = new System.Collections.Generic.HashSet<string>();

        private List<Dictionary<string, string>> transformTableRot(List<Dictionary<string, string>> table)
        {
            Dictionary<float, float> maxIntrusionProbPerrotationsCount = new Dictionary<float, float>();
            Dictionary<float, float> minIntrusionProbPerrotationsCount = new Dictionary<float, float>();
            Dictionary<float, float> avgIntrusionProbPerrotationsCount = new Dictionary<float, float>();
            Dictionary<float, int> occurancesPerrotationsCount = new Dictionary<float, int>();
            foreach (var s in table)
            {
                float rotationsCount = float.Parse(s["rotationsCount"]);
                float intrusionProb = float.Parse(s["maxIntrusionProbability"]);
                if (!maxIntrusionProbPerrotationsCount.ContainsKey(rotationsCount))
                {
                    maxIntrusionProbPerrotationsCount[rotationsCount] = intrusionProb;
                    minIntrusionProbPerrotationsCount[rotationsCount] = intrusionProb;
                    avgIntrusionProbPerrotationsCount[rotationsCount] = intrusionProb;
                    occurancesPerrotationsCount[rotationsCount] = 1;
                }
                else
                {
                    maxIntrusionProbPerrotationsCount[rotationsCount] = Math.Max(intrusionProb, maxIntrusionProbPerrotationsCount[rotationsCount]);
                    minIntrusionProbPerrotationsCount[rotationsCount] = Math.Min(intrusionProb, minIntrusionProbPerrotationsCount[rotationsCount]);
                    avgIntrusionProbPerrotationsCount[rotationsCount] += intrusionProb;
                    ++occurancesPerrotationsCount[rotationsCount];
                }
            }
            foreach (float rotationsCount in maxIntrusionProbPerrotationsCount.Keys)
            {
                avgIntrusionProbPerrotationsCount[rotationsCount] /= occurancesPerrotationsCount[rotationsCount];
            }

            var res = new List<Dictionary<string, string>>();
            foreach (float rotationsCount in maxIntrusionProbPerrotationsCount.Keys)
            {
                var line = new Dictionary<string, string>();
                line["rotationsCount"] = rotationsCount.ToString();
                line["MaxIntrusionProb"] = maxIntrusionProbPerrotationsCount[rotationsCount].ToString();
                line["MinIntrusionProb"] = minIntrusionProbPerrotationsCount[rotationsCount].ToString();
                line["AvgIntrusionProb"] = avgIntrusionProbPerrotationsCount[rotationsCount].ToString();
                res.Add(line);
            }
            return res;
        }
        private List<Dictionary<string, string>> transformTableDB(List<Dictionary<string, string>> table)
        {
            Dictionary<float, float> maxIntrusionProbPerDb = new Dictionary<float, float>();
            Dictionary<float, float> minIntrusionProbPerDb = new Dictionary<float, float>();
            Dictionary<float, float> avgIntrusionProbPerDb = new Dictionary<float, float>();
            Dictionary<float, int> occurancesPerDb = new Dictionary<float, int>();
            foreach (var s in table)
            {
                float Db = float.Parse(s["Db"]);
                float intrusionProb = float.Parse(s["maxIntrusionProbability"]);
                if (!maxIntrusionProbPerDb.ContainsKey(Db))
                {
                    maxIntrusionProbPerDb[Db] = intrusionProb;
                    minIntrusionProbPerDb[Db] = intrusionProb;
                    avgIntrusionProbPerDb[Db] = intrusionProb;
                    occurancesPerDb[Db] = 1;
                }
                else
                {
                    maxIntrusionProbPerDb[Db] = Math.Max(intrusionProb, maxIntrusionProbPerDb[Db]);
                    minIntrusionProbPerDb[Db] = Math.Min(intrusionProb, minIntrusionProbPerDb[Db]);
                    avgIntrusionProbPerDb[Db] += intrusionProb;
                    ++occurancesPerDb[Db];
                }
            }
            foreach (float Db in maxIntrusionProbPerDb.Keys)
            {
                avgIntrusionProbPerDb[Db] /= occurancesPerDb[Db];
            }

            var res = new List<Dictionary<string, string>>();
            foreach (float Db in maxIntrusionProbPerDb.Keys)
            {
                var line = new Dictionary<string, string>();
                line["Db"] = Db.ToString();
                line["MaxIntrusionProb"] = maxIntrusionProbPerDb[Db].ToString();
                line["MinIntrusionProb"] = minIntrusionProbPerDb[Db].ToString();
                line["AvgIntrusionProb"] = avgIntrusionProbPerDb[Db].ToString();
                res.Add(line);
            }
            return res;
        }
        private List<Dictionary<string, string>> transformTablePB(List<Dictionary<string, string>> table)
        {
            Dictionary<float, float> maxIntrusionProbPerPb = new Dictionary<float, float>();
            Dictionary<float, float> minIntrusionProbPerPb = new Dictionary<float, float>();
            Dictionary<float, float> avgIntrusionProbPerPb = new Dictionary<float, float>();
            Dictionary<float, int> occurancesPerPb = new Dictionary<float, int>();
            foreach (var s in table)
            {
                float pb = float.Parse(s["Pb"]);
                float intrusionProb = float.Parse(s["maxIntrusionProbability"]);
                if (!maxIntrusionProbPerPb.ContainsKey(pb))
                {
                    maxIntrusionProbPerPb[pb] = intrusionProb;
                    minIntrusionProbPerPb[pb] = intrusionProb;
                    avgIntrusionProbPerPb[pb] = intrusionProb;
                    occurancesPerPb[pb] = 1;
                }
                else
                {
                    maxIntrusionProbPerPb[pb] = Math.Max(intrusionProb, maxIntrusionProbPerPb[pb]);
                    minIntrusionProbPerPb[pb] = Math.Min(intrusionProb, minIntrusionProbPerPb[pb]);
                    avgIntrusionProbPerPb[pb] += intrusionProb;
                    ++occurancesPerPb[pb];
                }
            }
            foreach (float pb in maxIntrusionProbPerPb.Keys)
            {
                avgIntrusionProbPerPb[pb] /= occurancesPerPb[pb];
            }

            var res = new List<Dictionary<string, string>>();
            foreach (float pb in maxIntrusionProbPerPb.Keys)
            {
                var line = new Dictionary<string, string>();
                line["Pb"] = pb.ToString();
                line["MaxIntrusionProb"] = maxIntrusionProbPerPb[pb].ToString();
                line["MinIntrusionProb"] = minIntrusionProbPerPb[pb].ToString();
                line["AvgIntrusionProb"] = avgIntrusionProbPerPb[pb].ToString();
                res.Add(line);
            }
            return res;
        }
        /// <summary>
        /// a quick fix that transforms all loaded series
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private List<Dictionary<string,string>> transformTable(List<Dictionary<string, string>> table)
        {
            try
            {
                return transformTableRot(table);
            }
            catch(Exception)
            {
                return table;
            }
        }
        private void addSeries(List<Dictionary<string, string>> table, string seriesName)
        {
            
            string selectedLstX = null; 
            string selectedLstY = null; 


            chartFileCaptions.Add(seriesName);
            grphLstChartFiles.Items.Add(seriesName);
                
            if (grphLstChartFiles.Items.Count == 1)  // first series
            {
                foreach (string s in table.First().Keys)
                    commonColumns.Add(s);

                try
                {
                    selectedLstX = AppSettings.Load(AppConstants.GUIDefaults.UTIL_CHART_X_AXIS_PARAM.key);
                    selectedLstY = AppSettings.Load(AppConstants.GUIDefaults.UTIL_CHART_Y_AXIS_PARAM.key);
                }
                catch (Exception) { }
            }
            else
            {
                // remove unshared columns:
                List<string> toRemove = new System.Collections.Generic.List<string>();
                foreach (string s in commonColumns)
                    if (!table.First().ContainsKey(s))
                        toRemove.Add(s);
                foreach (string s in toRemove)
                    commonColumns.Remove(s);

                selectedLstX = (string)lstX.SelectedItem;
                selectedLstY = (string)lstY.SelectedItem;
            }



            lstX.Items.Clear();
            lstY.Items.Clear();
                
            foreach (string s in commonColumns)
            {
                lstX.Items.Add(s);
                lstY.Items.Add(s);
            }

            try
            {
                trySelecting(lstX, selectedLstX);
                trySelecting(lstY, selectedLstY);
            }
            catch (Exception) { }

        }
        void trySelecting(ListBox target, string val)
        {
            if (val == null)
                return;
            for (int i = 0; i < target.Items.Count; ++i)
                if ((string)(target.Items[i]) == val)
                {
                    target.SelectedIndex = i;
                    break;
                }
        }
        private void grphBtnAddChartFile_Click(object sender, System.EventArgs e)
        {

            OpenFileDialog f = new System.Windows.Forms.OpenFileDialog();
            f.Multiselect = true;
            f.Title = "Select series (result/theory) file";
            if (f.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                foreach (string filename in f.FileNames)
                {
                    List<string> res = InputBox.ShowDialog("series name", "", filename.Split(new char[1] { '\\' }).Last());

                    var table = Utils.FileUtils.readTable(filename);
                    table = transformTable(table);
                    chartTables.Add(table);
                    addSeries(table,res[0]);
                }
                updatePreview();
            }
        }

        private void grphBtnClearAllSeries_Click(object sender, System.EventArgs e)
        {
            chartTables.Clear();
            chartFileCaptions.Clear();
            grphLstChartFiles.Items.Clear();
            commonColumns.Clear();
        }

        private void grphBtnGenerateChart_Click(object sender, System.EventArgs e)
        {
            SaveFileDialog s = new System.Windows.Forms.SaveFileDialog();
            s.Title = "output destination";
            s.Filter = "png Files (*.png)|*.png";
            if (s.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            
            
            var res = InputBox.ShowDialog(new string[] { "x label:", "y label:" }, "diagram properties",
                new string[] { (string)lstX.SelectedItem, (string)lstY.SelectedItem });

            try
            {
                makeChart(chartTables, chartFileCaptions, s.FileName, (string)lstX.SelectedItem, (string)lstY.SelectedItem, res[0], res[1]);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void grphBtnRenameSeries_Click(object sender, System.EventArgs e)
        {
            List<string> res = InputBox.ShowDialog("series name", "", (string)grphLstChartFiles.SelectedItem);
            if (res != null)
            {
                chartFileCaptions[grphLstChartFiles.SelectedIndex] = res[0];
                grphLstChartFiles.Items[grphLstChartFiles.SelectedIndex] = res[0];
            }
        }

        private void frmUtils_Load(object sender, System.EventArgs e)
        {
            //var expressionForm = new DynamicCompilation.BasicExecution();
            //expressionForm.TopLevel = false;
            //expressionForm.AutoScroll = true;
            //tabDispExpressions.Controls.Add(expressionForm);
            //expressionForm.Show();
        }
        

        private void btnOpenExpressions_Click(object sender, System.EventArgs e)
        {

#if MONO
            MessageBox.Show("avilable on windows only :(");
            return;
#else
            object expressionObj = null;
            btnOpenExpressions.Visible = false;
            btnChooseSandbox.Visible = false;
            
            List<string> myAssemblyName = new System.Collections.Generic.List<string>();
            //myAssemblyName.Add(Assembly.GetExecutingAssembly().FullName);
            
            
            var frm = new BasicExecution(handleExpressionObject, myAssemblyName);
            frm.Show();

            frm.FormClosed += 
                (object s, System.Windows.Forms.FormClosedEventArgs ea)=>{
                    btnOpenExpressions.Visible = true;
                    btnChooseSandbox.Visible = true;
                    btnDisplaySandbox.Visible = false;
                    expressionObj = null;
                };
#endif
        }

        object expressionObj = null;
        private void handleExpressionObject(string message, object resObj)
        {
            if (resObj == null)
            {
                MessageBox.Show(message);
                return;
            }
            expressionObj = resObj;
            inputDirty = true;
            tabDispExpressions.Invalidate();
        }

        private void drawMatrix<T>(AMatrix<T> o, System.Windows.Forms.PaintEventArgs e)
        {
            
            string matrixStr = "";
            for (int iRow = 0; iRow < o.rows; ++iRow)
            {

                for (int iCol = 0; iCol < o.cols; ++iCol)
                {
                    matrixStr += o[iRow, iCol].ToString() + "\t\t";
                }
                matrixStr += Environment.NewLine;
            }
            Point center = new Point(0, 0);//new Point(tabDispExpressions.Size.Width / 2, tabDispExpressions.Size.Height / 2);

            e.Graphics.DrawString(matrixStr,
                                  new System.Drawing.Font(System.Drawing.FontFamily.GenericMonospace,
                                                          10, System.Drawing.GraphicsUnit.Pixel),
                                  new SolidBrush(Color.Black),
                                  center);
        }
        private void drawStrings(List<string> sl, System.Windows.Forms.PaintEventArgs e)
        {
            Point center = new Point(0, 0);
            foreach (string s in sl)
            {
                e.Graphics.DrawString(s,
                                      new System.Drawing.Font(System.Drawing.FontFamily.GenericMonospace,
                                                              10, System.Drawing.GraphicsUnit.Pixel),
                                      new SolidBrush(Color.Black),
                                      center);
                center.Y += 15;
            }
        }
        private void drawGraph(Dictionary<string,AFunctionTreeNode> func, double minParam, double maxParam,
            System.Windows.Forms.PaintEventArgs e)
        {            
            e.Graphics.DrawImage(
                makeFuncChart(tabDispExpressions.Size.Width, tabDispExpressions.Size.Height, func, minParam, maxParam),
                new Point(0, 0));
        }

        private void drawGraph(AFunctionTreeNode func, double minParam, double maxParam,
            System.Windows.Forms.PaintEventArgs e)
        {
            Dictionary<string, AFunctionTreeNode> funcDic = new System.Collections.Generic.Dictionary<string, AFunctionTreeNode>();
            funcDic["Function"] = func;

            e.Graphics.DrawImage(
                makeFuncChart(tabDispExpressions.Size.Width, tabDispExpressions.Size.Height, funcDic, minParam, maxParam),
                new Point(0, 0));
        }

        bool inputDirty = false;
        int minX, maxX;
        Dictionary<string, AFunctionTreeNode> res;
        private void tabDispExpressions_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            /// handled object types:
            /// MatrixD
            /// MatrixOpTree
            /// AFunctionTreeNode (or a List<> of them) will be displayed as a graph (min/max range will be asked in input box)
            /// Tuple<string, List<PointF>> (or a List<> of tuples) will be displayed as graph
            
            if (expressionObj == null)
                return;
            
            e.Graphics.TranslateTransform(-hscrlExp.Value * 10,vscrlExp.Value * 10);
            
            try
            {
                // TODO: maybe move to Dictionary instead of if/else list?
                if (expressionObj is MatrixD)
                    drawMatrix((MatrixD)expressionObj, e);
                else if (expressionObj is MatrixOpTree)
                    drawMatrix((MatrixOpTree)expressionObj, e);
                else if (expressionObj is AFunctionTreeNode)
                {
                    if (inputDirty)
                    {
                        var inpRes =
                            InputBox.ShowDialog<int>(new string[] { "min X", "max X", "Sample Diff" }, "draw functions Param", new string[] { "0", "1", "0.02" });
                        minX = inpRes["min X"];
                        maxX = inpRes["max X"];
                    }

                    drawGraph((AFunctionTreeNode)expressionObj, minX, maxX, e);
                }
                else if (expressionObj is List<AFunctionTreeNode>)
                {
                    if (inputDirty)
                    {
                        List<AFunctionTreeNode> tmpL = (List<AFunctionTreeNode>)expressionObj;
                        res = new System.Collections.Generic.Dictionary<string, AFunctionTreeNode>();
                        int i = 0;
                        foreach (var v in tmpL)
                        {
                            res[InputBox.ShowDialog("name func " + i.ToString(), "", i.ToString()).Last()] = v;
                            ++i;
                        }

                        var inpRes =
                            InputBox.ShowDialog<int>(new string[] { "min X", "max X" , "Sample Diff"}, "draw functions Param", new string[] { "0", "1" , "0.02"});
                        minX = inpRes["min X"];
                        maxX = inpRes["max X"];
                    }
                    drawGraph(res, minX, maxX, e);
                }
                else
                {
                    List<string> sl = new System.Collections.Generic.List<string>();
                    sl.Add(expressionObj.ToString());
                    drawStrings(sl, e);
                }

                
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            inputDirty = false; // even if there is an exception, we won't ask for input again untill another 'execute code'
                                //call is done
        }

        private void btnChooseSandbox_Click(object sender, System.EventArgs e)
        {
            try
            {
                btnDisplaySandbox.Visible = false;
                List<string> res = InputBox.ShowDialog("SanboxClassName", "SanboxClassName");
                activeBox = ASandBox.ChildrenByTypename[res.First()];
                btnOpenExpressions.Visible = false;
                btnChooseSandbox.Visible = false;
                btnDisplaySandbox.Visible = true;
            }
            catch (Exception) 
            {
                MessageBox.Show("Can't find class name /  class name is not of type ASandBox");
            }
        }
        ASandBox activeBox = null;
        private void btnDisplaySandbox_Click(object sender, System.EventArgs e)
        {
            handleExpressionObject(activeBox.func().ToString(), activeBox.func());
        }

        private void tabDispExpressions_Click(object sender, System.EventArgs e)
        {

        }

        private void tabDispExpressions_Enter(object sender, System.EventArgs e)
        {
            btnOpenExpressions.Visible = true;
            btnChooseSandbox.Visible = true;
            btnDisplaySandbox.Visible = false;
            expressionObj = null;
        }

        private void sizablePanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void hscrlExp_Scroll(object sender, ScrollEventArgs e)
        {
            tabDispExpressions.Invalidate();
        }
        
        private void updatePreview(List<Dictionary<string, string>> chartTable = null, string chartFileCaption = null)
        {
            try
            {
                if (chartTable == null)
                {
                    pictureChartOutPreview.Image =
                        makeChart(chartTables, chartFileCaptions, null, (string)lstX.SelectedItem, (string)lstY.SelectedItem, (string)lstX.SelectedItem, (string)lstY.SelectedItem);
                }
                else
                {
                    List<string> tmpCaptions = new List<string>();
                    List<List<Dictionary<string, string>>> tmpchartTables = new List<List<Dictionary<string, string>>>();
                    tmpchartTables.Add(chartTable);
                    tmpCaptions.Add(chartFileCaption);
                    pictureChartOutPreview.Image =
                        makeChart(tmpchartTables, tmpCaptions, null, (string)lstX.SelectedItem, (string)lstY.SelectedItem, (string)lstX.SelectedItem, (string)lstY.SelectedItem);
                }
            }
            catch (Exception) {}
        }

        private void btnClipboardSeries_Click(object sender, EventArgs e)
        {
            try
            {
                string tableTxt = Clipboard.GetText();

                var table = ParsingUtils.readTable(ParsingUtils.splitToLines(tableTxt).ToArray());
                table = transformTable(table);

                chartTables.Add(table);
                List<string> res = InputBox.ShowDialog("series name", "", "series" + chartTables.Count.ToString());
                addSeries(chartTables.Last(), res[0]);
                updatePreview();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void lstX_SelectedIndexChanged(object sender, EventArgs e)
        {
            updatePreview();

            if ((string)lstX.SelectedItem != null)
            {
                try{
                    AppSettings.Save(AppConstants.GUIDefaults.UTIL_CHART_X_AXIS_PARAM.key, (string)lstX.SelectedItem);
                }
                catch (Exception) { }
            }
        }

        private void lstY_SelectedIndexChanged(object sender, EventArgs e)
        {
            updatePreview();

            if ((string)lstY.SelectedItem != null)
            {
                try
                {
                    AppSettings.Save(AppConstants.GUIDefaults.UTIL_CHART_Y_AXIS_PARAM.key, (string)lstY.SelectedItem);
                }
                catch (Exception) { }
            }
        }

        private void grphLstChartFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                updatePreview(chartTables[grphLstChartFiles.SelectedIndex],chartFileCaptions[grphLstChartFiles.SelectedIndex]);
            }
            catch (Exception) { }
        }

        private void tabResultGraphs_Click(object sender, EventArgs e)
        {
            
        }

        private void btn_Click(object sender, EventArgs e)
        {
            OpenFileDialog f = new System.Windows.Forms.OpenFileDialog();
            f.Multiselect = true;
            f.Title = "Select SQLite query output csv file";
            if (f.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                
                foreach (string filename in f.FileNames)
                {
                    List<string> newFileLines = new List<string>();
                    if(txtFirstRowFieldNames.Text != "")
                        newFileLines.Add(txtFirstRowFieldNames.Text);

                    var fileLines = File.ReadAllLines(filename);
                    newFileLines.AddRange(fileLines.Select((s)=>cleanSqliteCSVLine(s)));
                    
                    if (chkBackupCSV.Checked)
                    {
                        string postfix, foldername;
                        Utils.FileUtils.getFolderOfFile(filename, out foldername, out postfix);
                        string prefix = "_"; 
                        while(true)
                        {
                            try
                            {
                                File.Copy(filename, foldername + "\\" + prefix + postfix);
                            }
                            catch(Exception)
                            {
                                prefix += "_";
                            }
                        }
                        
                    }
                    File.WriteAllLines(filename, newFileLines.ToArray());
                }
            }
        }

        private string cleanSqliteCSVLine(string s)
        {
            if (s.IndexOf('"') == -1)
                return s;
            string cleanString = 
                s.Substring(s.IndexOf('"') + 1, s.Length - 2); // remove first and last ' " '
            return cleanString.Replace("\",\"", ","); // all values have qoutations, so this cleans it
        }

        private void button1_Click(object sender, EventArgs e)
        {
            LPSolver55.MyDemo.Test();
            LPSolver55.DemoEx.Demo();
            //LPSolver55.demo.test();

        }

        private void btnMrg_Click(object sender, EventArgs e)
        {
            OpenFileDialog f = new System.Windows.Forms.OpenFileDialog();
            f.Multiselect = true;
            f.Title = "Select series (result/theory) file";
            if (f.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                List<Dictionary<string, string>> mergedtable = new List<Dictionary<string, string>>();
                foreach (string filename in f.FileNames)
                {
                    var table = Utils.FileUtils.readTable(filename);
                    mergedtable.AddRange(table);
                }
                List<string> res = InputBox.ShowDialog("series name", "", f.FileNames.First().Split(new char[1] { '\\' }).Last());
                mergedtable = transformTable(mergedtable);
                chartTables.Add(mergedtable);
                addSeries(mergedtable, res[0]);
                updatePreview();
            }
        }

        private void vscrlExp_Scroll(object sender, ScrollEventArgs e)
        {
            tabDispExpressions.Invalidate();
        }
        

    }
}
