using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BitMiracle.LibTiff.Classic;
using Warp;
using Warp.Headers;
using Warp.Tools;
using System.Xml;
using System.Xml.XPath;
using System.Globalization;

namespace Warp
{
    class AFISstar : Star
    {
        private string basepath = "";
        private int nclusters = 10;
        private Dictionary<string, int> micrographs = new Dictionary<string, int>();
        public AFISstar(string path, string tableName = "") : base(path, tableName)
        {
            basepath = Path.GetDirectoryName(path);
        }
        public AFISstar(string[] columnNames) : base(columnNames) { }
        public AFISstar(Star[] tables) : base(tables) { }
        public string GetXMLfile(string micrograph, string microscopeXMLpath)
        {
            return microscopeXMLpath + micrograph.Substring(0, 56) + ".xml";
        }
        public double[] getShifts(string micrograph, string microscopeXMLpath)
        {
            microscopeXMLpath = Path.GetFullPath(microscopeXMLpath).TrimEnd(Path.DirectorySeparatorChar) + "\\";
            string metadataXMLfile = GetXMLfile(micrograph, microscopeXMLpath);
            if (File.Exists(metadataXMLfile))
            {
                using (Stream SettingsStream = File.OpenRead(metadataXMLfile))
                {
                    XPathDocument document = new XPathDocument(SettingsStream);
                    XPathNavigator navigator = document.CreateNavigator();

                    XmlNamespaceManager manager = new XmlNamespaceManager(navigator.NameTable);
                    manager.AddNamespace("mns", "http://schemas.datacontract.org/2004/07/Fei.SharedObjects");
                    manager.AddNamespace("i", "http://www.w3.org/2001/XMLSchema-instance");
                    manager.AddNamespace("a", "http://schemas.datacontract.org/2004/07/Fei.Types");

                    string xpath = "//mns:BeamShift/a:_x";
                    string xvalue = navigator.SelectSingleNode(xpath, manager).Value;
                    xpath = "//mns:BeamShift/a:_y";
                    string yvalue = navigator.SelectSingleNode(xpath, manager).Value;
                    return new double[] { double.Parse(xvalue, CultureInfo.InvariantCulture), double.Parse(yvalue, CultureInfo.InvariantCulture) };
                }
            }
            else
            {
                return new double[] { double.Parse("0", CultureInfo.InvariantCulture), double.Parse("0", CultureInfo.InvariantCulture) };
            }
        }
        public List<string> getMicrographList()
        {
            List<string> allmic = new List<string>();
            for (int i = 0; i < Rows.Count; i++)
            {
                allmic.Add(GetRowValue(i, "rlnMicrographName"));
            }

            List<string> m = allmic.Distinct().ToList();
            return (m);
        }
        public double micrographToOpticsGroup(string microscopeXMLpath, int ngroups = 10, int nmics = 0)
        {
            List<string> m = getMicrographList();
            int count = 0;
            if (nmics > 0)
            {
                count = nmics;
            }
            else
            {
                count = m.Count;
            }

            double[,] shifts = new double[count, 2];

            for (int i = 0; i < count; i++)
            {
                double[] shift = getShifts(m[i], microscopeXMLpath);
                shifts[i, 0] = shift[0];
                shifts[i, 1] = shift[1];
            }

            alglib.clusterizerstate s;
            alglib.kmeansreport rep;

            alglib.clusterizercreate(out s);
            alglib.clusterizersetpoints(s, shifts, 2);
            alglib.clusterizersetkmeanslimits(s, 5, 0);
            alglib.clusterizerrunkmeans(s, ngroups, out rep);

            double scores = 0;
            micrographs.Clear();
            for (int i = 0; i < count; i++)
            {
                micrographs.Add(m[i], rep.cidx[i]);
                double dist = Math.Sqrt(Math.Pow((shifts[i, 0] - rep.c[rep.cidx[i], 0]), 2) + Math.Pow((shifts[i, 1] - rep.c[rep.cidx[i], 1]), 2));
                scores += dist;
            }
            return (scores);
        }
        public void AddOpticGroups(string microscopeXMLpath, int newnclusters = 10, int shiftby=0)
        {
            nclusters = newnclusters;
            micrographToOpticsGroup(microscopeXMLpath, nclusters);
            string[] groups = new string[Rows.Count];
            int value = 0;
            for (int i = 0; i < Rows.Count; i++)
            {
                micrographs.TryGetValue(GetRowValue(i, "rlnMicrographName"), out value);
                value += shiftby;
                groups[i] = value.ToString();
            }
            AddColumn("rlnOpticsGroup", groups);
        }
        public int estimateNumberOfOpticsGroups(int nmicrographs, string microscopeXMLpath)
        {
            List<double> scores = new List<double>();
            for (int i = 2; i < 20; i++)
            {
                scores.Add(micrographToOpticsGroup(microscopeXMLpath, i, nmicrographs));
            }

            List<double> angles = new List<double>();
            int maxangleindex = 0;
            double smallestangle = Math.PI;
            for (int i = 1; i < scores.Count - 1; i++)
            {
                double[] p1 = new double[2] { scores[i - 1], i - 1.0 };
                double[] p2 = new double[2] { scores[i], i + 0.0 };
                double[] p3 = new double[2] { scores[i + 1], i + 1.0 };
                double angle = GetAngle(p1, p2, p3);
                angles.Add(angle);
                if (angle < smallestangle)
                {
                    smallestangle = angle;
                    maxangleindex = i;
                }
            }
            nclusters = maxangleindex + 2;
            Console.WriteLine("Estimated number of optics groups: {0}", nclusters.ToString());
            return (nclusters);
        }
        public static double GetAngle(double[] p1, double[] p2, double[] p3)
        {
            double lenghtA = Math.Sqrt(Math.Pow(p2[0] - p1[0], 2) + Math.Pow(p2[1] - p1[1], 2));
            double lenghtB = Math.Sqrt(Math.Pow(p3[0] - p2[0], 2) + Math.Pow(p3[1] - p2[1], 2));
            double lenghtC = Math.Sqrt(Math.Pow(p3[0] - p1[0], 2) + Math.Pow(p3[1] - p1[1], 2));

            double calc = ((lenghtA * lenghtA) + (lenghtB * lenghtB) - (lenghtC * lenghtC)) / (2 * lenghtA * lenghtB);

            return Math.Acos(calc);
        }
        public void Save(string path)
        {
            using (TextWriter Writer = File.CreateText(path))
            {
                Writer.WriteLine("");
                Writer.WriteLine("data_");
                Writer.WriteLine("");
                Writer.WriteLine("loop_");

                foreach (var pair in NameMapping)
                    Writer.WriteLine($"_{pair.Key} #{pair.Value + 1}");

                foreach (var row in Rows)
                    Writer.WriteLine("  " + string.Join("  ", row));
            }
        }
    }
}

/*
             * loop_
_rlnOpticsGroupName #1 
_rlnOpticsGroup #2 
_rlnMtfFileName #3 
_rlnMicrographOriginalPixelSize #4 
_rlnVoltage #5 
_rlnSphericalAberration #6 
_rlnAmplitudeContrast #7 */
