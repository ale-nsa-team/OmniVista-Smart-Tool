using PoEWizard.Data;
using System;
using System.Collections.Generic;

namespace PoEWizard.Device
{
    public class PortTrafficModel
    {
        public string Port { get; set; }
        public string MacAddress { get; set; }
        public double BandWidth { get; set; }
        public List<double> RxBytes { get; set; }
        public List<double> TxBytes { get; set; }
        public List<double> UnicastFrames { get; set; }
        public List<double> BroadcastFrames { get; set; }
        public List<double> MulticastFrames { get; set; }
        public List<double> LostFrames { get; set; }
        public List<double> CrcErrorFrames { get; set; }
        public List<double> AlignmentsError { get; set; }
        public List<double> CollidedFrames { get; set; }
        public List<double> Collisions { get; set; }
        public List<double> LateCollisions { get; set; }
        public List<double> ExcCollisions { get; set; }

        #region Unused properties
        public string LinkQuality { get; set; }
        public double LongFrameSize { get; set; }
        public double InterFrameGap { get; set; }
        public double UnderSizeFrames { get; set; }
        public double OverSizeFrames { get; set; }
        public double ErrorFrames { get; set; }
        #endregion

        public PortTrafficModel(Dictionary<string, string> dict)
        {
            RxBytes = new List<double>();
            TxBytes = new List<double>();
            UnicastFrames = new List<double>();
            BroadcastFrames = new List<double>();
            MulticastFrames = new List<double>();
            LostFrames = new List<double>();
            CrcErrorFrames = new List<double>();
            AlignmentsError = new List<double>();
            CollidedFrames = new List<double>();
            Collisions = new List<double>();
            LateCollisions = new List<double>();
            ExcCollisions = new List<double>();
            UpdateTraffic(dict);
        }

        public void UpdateTraffic(Dictionary<string, string> dict)
        {
            Port = Utils.GetDictValue(dict, Constants.PORT);
            MacAddress = Utils.GetDictValue(dict, Constants.TRAF_MAC_ADDRESS);
            RxBytes.Add(Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_RX_BYTES)));
            TxBytes.Add(Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_TX_BYTES)));
            BandWidth = Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_BANDWIDTH));
            UnicastFrames.Add(Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_UNICAST_FRAMES)));
            BroadcastFrames.Add(Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_BROADCAST_FRAMES)));
            MulticastFrames.Add(Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_MULTICAST_FRAMES)));
            LostFrames.Add(Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_LOST_FRAMES)));
            CrcErrorFrames.Add(Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_CRC_ERROR_FRAMES)));
            AlignmentsError.Add(Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_ALIGNEMENTS_ERROR)));
            CollidedFrames.Add(Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_COLLIDED_FRAMES)));
            Collisions.Add(Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_COLLISIONS)));
            LateCollisions.Add(Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_LATE_COLLISIONS)));
            Collisions.Add(Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_EXC_COLLISIONS)));
            #region Unused properties
            LinkQuality = Utils.GetDictValue(dict, Constants.TRAF_LINK_QUALITY);
            LongFrameSize = Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_LONG_FRAME_SIZE));
            InterFrameGap = Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_INTER_FRAME_GAP));
            UnderSizeFrames = Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_UNDERSIZE_FRAMES));
            OverSizeFrames = Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_OVERSIZE_FRAMES));
            ErrorFrames = Utils.StringToDouble(Utils.GetDictValue(dict, Constants.TRAF_ERROR_FRAMES));
            #endregion
        }

    }

}
