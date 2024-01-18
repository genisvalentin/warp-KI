using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.Statistics;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Numerics;
using Accord.MachineLearning;


namespace Warp
{
    public class OpticGroups
    {
        public static Coord RotatePoint(Coord p, double theta)
        {
            double c = Math.Cos(theta);
            double s = Math.Sin(theta);
            return new Coord(c * p.X - s * p.Y, s * p.X + c * p.Y);
        }

        public static Coord[] RotateArray(Coord[] coords, double theta)
        {
            Coord[] result = new Coord[coords.Length];
            for (int i = 0; i < coords.Length; i++)
            {
                var p = RotatePoint(coords[i], theta);
                result[i] = p;
            }
            return result;
        }

        public static double[,] GetHistogram(double[] input)
        {
            int nbins = 500;
            var histogram = new Histogram(input, nbins);
            double[,] result = new double[nbins, 2];
            for (int bin = 0; bin < nbins; bin++)
            {
                result[bin, 0] = histogram[bin].LowerBound + histogram[bin].Width / 2;
                result[bin, 1] = histogram[bin].Count;
            }
            return (result);
        }

        public static double[] FindFrequency(Coord[] coords, int column = 0, int bin = 25)
        {
            var a = coords.ToList().Select(x => column == 0 ? x.X : x.Y).ToArray();
            var hist = GetHistogram(a);
            var xhist = Enumerable.Range(0, hist.GetLength(0) / 2).Select(i => hist[i, 0]).ToArray();
            var yhist = Enumerable.Range(0, hist.GetLength(0)).Select(i => new Complex32((float)hist[i, 1], 0)).ToArray();
            Fourier.Forward(yhist);

            var samplingFrequency = Math.Abs(xhist.Length / (xhist.Last() - xhist.First()));
            var tpCount = xhist.Length;
            var timePeriod = tpCount / samplingFrequency;

            var transform = Enumerable.Range(2, tpCount / bin).Select(i => yhist[i].Magnitude / yhist.Length).ToList();
            var frequencies = Enumerable.Range(2, tpCount / bin).Select(i => i / timePeriod).Where(i => i > 2).ToList();

            var idx = transform.IndexOf(transform.Max());
            var frequency = frequencies[idx];
            var amplitude = transform[idx];

            return (new double[] { 1 / frequency * 2, timePeriod, amplitude });
        }

        public static Vector2 GetVectors(Coord[] incoords, int search_range, int column = 0, int bin = 25, int start_angle = 0)
        {
            double[,] r = new double[search_range, 3]; //angle : freq, sampling, amplitude
            Coord[] transformed_array;
            var coords = RotateArray(incoords, start_angle * Math.PI / 180.0);
            for (var angle = 0; angle < search_range; angle++)
            {
                transformed_array = RotateArray(coords, angle * Math.PI / 180.0);
                var findFreq = FindFrequency(transformed_array, column, bin);
                for (var i = 0; i < 3; i++)
                {
                    r[angle, i] = findFreq[i];
                }
            }

            List<int> SubsetIdxs = new List<int>();
            for (int i = 0; i < r.GetLength(0); i++)
            {
                if (r[i, 0] < 20) SubsetIdxs.Add(i);
            }

            if (SubsetIdxs.Count == 0) return (new Vector2());

            int maxAngle = 0;
            double maxValue = r[0, 2];
            foreach (int i in SubsetIdxs)
            {
                if (r[i, 2] > maxValue)
                {
                    maxValue = r[i, 2];
                    maxAngle = i;
                }
            }

            var p = column == 0 ? new Coord(r[maxAngle, 0], 0) : new Coord(0, r[maxAngle, 0]);
            maxAngle += start_angle;
            p = RotatePoint(p, -maxAngle * Math.PI / 180.0);

            return (new Vector2((float)p.X, (float)p.Y));
        }

        //Takes a double array with the shift, the angle, the frequency, the spacing and the amplitude after fourier fitting
        public static int[][] GetCentroidIndexes(Coord[] coords, Vector2 v1, Vector2 v2)
        {
            var x = coords.ToList().Select(i => i.X).ToArray();
            var y = coords.ToList().Select(i => i.Y).ToArray();
            double minx = x.Min();
            double maxx = x.Max();
            double miny = y.Min();
            double maxy = y.Max();

            int xrange = (int)Math.Abs(Math.Floor((maxx - minx) / v1.X / 2) + 2);
            int yrange = (int)Math.Abs(Math.Floor((maxy - miny) / v2.Y / 2) + 2);

            List<int[]> indexesList = new List<int[]>();

            for (int i = -xrange; i < xrange; i++)
            {
                for (int j = -yrange; j < yrange; j++)
                {
                    indexesList.Add(new int[] { i, j });
                }
            }

            var usedIndexes = EliminateEmptyIndexes(coords, indexesList.ToArray(), v1, v2);
            return (usedIndexes);
        }

        public static int[][] EliminateEmptyIndexes(Coord[] coords, int[][] indexes, Vector2 v1, Vector2 v2)
        {
            var c = GetCentroids(indexes, v1, v2);

            double[][] providedCentroids = c.Select(x => new double[] { x.X, x.Y }).ToArray();

            var kmeans = new KMeans(k: providedCentroids.Length);
            kmeans.Centroids = providedCentroids;

            var idxs = kmeans.Clusters.Decide(coords.Select(x => new double[] { x.X, x.Y }).ToArray()).Distinct().ToList();

            int[][] usedIndexes = new int[idxs.Count][];
            int counter = 0;
            foreach (int i in idxs)
            {
                usedIndexes[counter] = indexes[i];
                counter++;
            }

            return usedIndexes;
        }

        public static Coord[] GetCentroids(int[][] indexes, Vector2 v1, Vector2 v2, Vector2 shift = new Vector2())
        {
            Coord[] centroids = new Coord[indexes.Length];
            for (int i = 0; i < indexes.Length; i++)
            {
                var mv1 = Vector2.Multiply(v1, indexes[i][0]);
                var mv2 = Vector2.Multiply(v2, indexes[i][1]);
                var sum = Vector2.Add(mv1, mv2);
                var shiftSum = Vector2.Add(sum, shift);
                centroids[i] = new Coord(shiftSum.X, shiftSum.Y);
            }
            return centroids;
        }

        public static Vector2[] RefineVectors(Coord[] coords, Vector2 v1, Vector2 v2)
        {
            var indexes = GetCentroidIndexes(coords, v1, v2);
            var centroids = GetCentroids(indexes, v1, v2);
            double[][] providedCentroids = centroids.Select(x => new double[] { x.X, x.Y }).ToArray();

            var kmeans = new KMeans(k: providedCentroids.Length);
            kmeans.Centroids = providedCentroids;
            var clusters = kmeans.Learn(coords.Select(x => new double[] { x.X, x.Y }).ToArray());

            List<Coord> refCentroids = new List<Coord>();
            foreach (var cluster in clusters.Centroids)
            {
                refCentroids.Add(new Coord(cluster[0], cluster[1]));
            }

            var unsortedxindexes = indexes.Select((v, i) => new { val = v, idx = i }).Where(x => x.val[0] == 0).ToList();
            unsortedxindexes.Sort((a, b) => a.val[1].CompareTo(b.val[1]));
            var xindexes = unsortedxindexes.Select(x => x.idx).ToList();

            var unsortedyindexes = indexes.Select((v, i) => new { val = v, idx = i }).Where(x => x.val[1] == 0).ToList();
            unsortedyindexes.Sort((a, b) => a.val[0].CompareTo(b.val[0]));
            var yindexes = unsortedyindexes.Select(x => x.idx).ToList();

            
            if (((indexes[xindexes.Last()][1] - indexes[xindexes.First()][1]) + 1 != xindexes.Count) && ((indexes[yindexes.Last()][0] - indexes[yindexes.First()][0]) + 1 != yindexes.Count))
            {
                return new Vector2[] { new Vector2(0, 0), new Vector2(0, 0) };
            } else if ((indexes[xindexes.Last()][1] - indexes[xindexes.First()][1]) + 1 != xindexes.Count)
            {
                return new Vector2[] { new Vector2(0, 0), v2 };
            } else if ((indexes[yindexes.Last()][0] - indexes[yindexes.First()][0]) + 1 != yindexes.Count)
            {
                return new Vector2[] { v1, new Vector2(0, 0) };
            }
            
            if (xindexes.Count < 3 && yindexes.Count < 3)
            {
                return new Vector2[] { new Vector2(0, 0), new Vector2(0, 0) };
            }
            else if (xindexes.Count < 3)
            {
                return new Vector2[] { new Vector2(0, 0), v2 };
            }
            else if (yindexes.Count < 3)
            {
                return new Vector2[] { v1, new Vector2(0, 0) };
            }

            List<Coord> xpoints = new List<Coord>();
            foreach (var i in xindexes.Take(3))
            {
                if (indexes[i][1] == 0) continue;
                xpoints.Add(new Coord(
                    clusters.Centroids[i][0] / indexes[i][1],
                    clusters.Centroids[i][1] / indexes[i][1])
                );
            }
            var xcentroid = GetCentroidFromCoordinates(xpoints.ToArray());

            List<Coord> ypoints = new List<Coord>();
            foreach (var i in yindexes.Take(5))
            {
                if (indexes[i][0] == 0) continue;
                ypoints.Add(new Coord(
                    clusters.Centroids[i][0] / indexes[i][0],
                    clusters.Centroids[i][1] / indexes[i][0])
                );
            }
            var ycentroid = GetCentroidFromCoordinates(ypoints.ToArray());

            var newV2 = new Vector2((float)xcentroid.X, (float)xcentroid.Y);
            var newV1 = new Vector2((float)ycentroid.X, (float)ycentroid.Y);
            return new Vector2[] { newV1, newV2 };
        }

        public static Coord GetCentroidFromCoordinates(Coord[] coords)
        {
            double sumx = 0;
            double sumy = 0;
            coords.ToList().ForEach(c => { sumx += c.X; sumy += c.Y; });
            return new Coord(sumx / coords.Length, sumy / coords.Length);
        }
    }

    public class Coord
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Coord(double x, double y)
        {
            X = x;
            Y = y;
        }

        // Calculate the distance between this point and another point
        public double Distance(Coord other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
