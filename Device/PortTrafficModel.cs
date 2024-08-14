using PoEWizard.Data;
using System;
using System.Collections.Generic;

namespace PoEWizard.Device
{
    public class PortTrafficModel
    {
        public string Port { get; set; }
        public string Status { get; set; }
        public string MacAddress { get; set; }
        public double BandWidth { get; set; }
        public double CurrRxBytes { get; set; }
        public double PrevRxBytes { get; set; }
        public double CurrTxBytes { get; set; }
        public double PrevTxBytes { get; set; }
        public double UnicastFrames { get; set; }
        public double BroadcastFrames { get; set; }
        public double MulticastFrames { get; set; }
        public double LostFrames { get; set; }
        public double CrcErrorFrames { get; set; }
        public double AlignmentsError { get; set; }
        public double CollidedFrames { get; set; }
        public double Collisions { get; set; }
        public double LateCollisions { get; set; }
        public double ExcCollisions { get; set; }
        public double SamplePeriod { get; set; }
        public double RxRate { get; set; }
        public double TxRate { get; set; }

        #region Unused properties
        public string LinkQuality { get; set; }
        public double LongFrameSize { get; set; }
        public double InterFrameGap { get; set; }
        public double UnderSizeFrames { get; set; }
        public double OverSizeFrames { get; set; }
        public double ErrorFrames { get; set; }
        #endregion

        public PortTrafficModel(Dictionary<string, string> dict, double duration)
        {
            UpdateTraffic(dict, duration);
        }

        public void UpdateTraffic(Dictionary<string, string> dict, double duration)
        {
            SamplePeriod = duration;
            Port = Utils.GetDictValue(dict, Constants.PORT);
            MacAddress = Utils.GetDictValue(dict, Constants.TRAF_MAC_ADDRESS);
            PrevRxBytes = CurrRxBytes;
            CurrRxBytes = Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_RX_BYTES));
            RxRate = CalculateTraffic(CurrRxBytes, PrevRxBytes);
            PrevTxBytes = CurrTxBytes;
            CurrTxBytes = Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_TX_BYTES));
            TxRate = CalculateTraffic(CurrTxBytes, PrevTxBytes);
            BandWidth = Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_BANDWIDTH));
            UnicastFrames = Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_UNICAST_FRAMES));
            BroadcastFrames = Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_BROADCAST_FRAMES));
            MulticastFrames = Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_MULTICAST_FRAMES));
            LostFrames = Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_LOST_FRAMES));
            CrcErrorFrames = Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_CRC_ERROR_FRAMES));
            AlignmentsError = Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_ALIGNEMENTS_ERROR));
            CollidedFrames = Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_COLLIDED_FRAMES));
            Collisions = Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_COLLISIONS));
            LateCollisions = Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_LATE_COLLISIONS));
            Collisions = Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_EXC_COLLISIONS));
            #region Unused properties
            LinkQuality = Utils.GetDictValue(dict, Constants.TRAF_LINK_QUALITY);
            LongFrameSize = Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_LONG_FRAME_SIZE));
            InterFrameGap = Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_INTER_FRAME_GAP));
            UnderSizeFrames = Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_UNDERSIZE_FRAMES));
            OverSizeFrames = Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_OVERSIZE_FRAMES));
            ErrorFrames = Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_ERROR_FRAMES));
            #endregion
        }

        private double CalculateTraffic(double currBytes, double prevBytes)
        {
            if (SamplePeriod < 5) return 0;
            if (prevBytes == 0) prevBytes = currBytes;
            double dVal = (currBytes - prevBytes) / SamplePeriod * 8 / 1048576;
            return Math.Round(dVal, 0, MidpointRounding.ToEven);
        }

    }

}
