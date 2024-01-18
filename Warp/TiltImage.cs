using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Warp.Controls;

namespace Warp
{
    public class TiltImage
    {
        private ProcessingStatus status = Controls.ProcessingStatus.Unprocessed;

        public int Zvalue { get; set; }

        public float TiltAngle { get; set; }

        public float CorrectedTiltAngle { get; set; }

        public FloatPair StagePosition { get; set; }

        public float StageZ { get; set; }

        public int Magnification { get; set; }

        public float Intensity { get; set; }

        public float ExposureDose { get; set; }

        public decimal PixelSpacing { get; set; }

        public int SpotSize { get; set; }

        public float Defocus { get; set; }

        public FloatPair ImageShift { get; set; }

        public float RotationAngle { get; set; }

        public float ExposureTime { get; set; }

        public int Binning { get; set; }

        public int MagIndex { get; set; }

        public string CountsPerElection { get; set; }

        public MinMaxMean MinMaxMean { get; set; }

        public float TargetDefocus { get; set; }

        public float PriorRecordDose { get; set; }

        public float UpdatedPriorRecordDose { get; set; }

        public string SubFramePath { get; set; }

        public int NumSubFrames { get; set; }

        public FloatPair FrameDosesAndNumber { get; set; }

        public FloatPair FilterSlitAndLoss { get; set; }

        public string ChannelName { get; set; }

        public string CameraLength { get; set; }

        public string ChannelLength { get; set; }

        public string DateTime { get; set; }

        public Controls.ProcessingStatus Status { get; set; }
    }

    public struct MinMaxMean
    {
        public float min;
        public float max;
        public float mean;

        public MinMaxMean(string input)
        {
            var floats = input.Split(' ').Select(x => float.Parse(x, CultureInfo.InvariantCulture)).ToList();
            min = floats[0];
            max = floats[1];
            mean = floats[1];
        }
    }

    public struct FloatPair
    {
        public float x;
        public float y;

        public FloatPair(string input)
        {
            var floats = input.Split(' ').Select(x => float.Parse(x, CultureInfo.InvariantCulture)).ToList();
            x = floats[0];
            y = floats[1];
        }
    }
}
