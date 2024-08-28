using PoEWizard.Data;
using System.Collections.Generic;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Device
{
    public class PortTrafficModel
    {
        public string Port { get; set; }
        public List<string> MacList { get; set; } = new List<string>();
        public string MacAddress { get; set; }
        public double BandWidth { get; set; }
        public List<double> RxBytes { get; set; } = new List<double>();
        public List<double> TxBytes { get; set; } = new List<double>();
        public List<double> RxUnicastFrames { get; set; } = new List<double>();
        public List<double> RxBroadcastFrames { get; set; } = new List<double>();
        public List<double> RxMulticastFrames { get; set; } = new List<double>();
        public List<double> RxLostFrames { get; set; } = new List<double>();
        public List<double> RxCrcErrorFrames { get; set; } = new List<double>();
        public List<double> RxAlignmentsError { get; set; } = new List<double>();
        public List<double> RxUnderSizeFrames { get; set; } = new List<double>();
        public List<double> RxOverSizeFrames { get; set; } = new List<double>();
        public List<double> RxErrorFrames { get; set; } = new List<double>();
        public List<double> TxUnicastFrames { get; set; } = new List<double>();
        public List<double> TxBroadcastFrames { get; set; } = new List<double>();
        public List<double> TxMulticastFrames { get; set; } = new List<double>();
        public List<double> TxUnderSizeFrames { get; set; } = new List<double>();
        public List<double> TxOverSizeFrames { get; set; } = new List<double>();
        public List<double> TxLostFrames { get; set; } = new List<double>();
        public List<double> TxCollidedFrames { get; set; } = new List<double>(); 
        public List<double> TxErrorFrames { get; set; } = new List<double>();
        public List<double> TxCollisions { get; set; } = new List<double>();
        public List<double> TxLateCollisions { get; set; } = new List<double>();
        public List<double> TxExcCollisions { get; set; } = new List<double>();

        #region Unused properties
        public string LinkQuality { get; set; }
        public double LongFrameSize { get; set; }
        public double InterFrameGap { get; set; }
        #endregion

        public PortTrafficModel(Dictionary<string, string> dict)
        {
            UpdateTraffic(dict);
        }

        public void UpdateTraffic(Dictionary<string, string> dict)
        {
            Port = Utils.GetDictValue(dict, PORT);
            MacAddress = Utils.GetDictValue(dict, TRAF_MAC_ADDRESS);
            RxBytes.Add(Utils.StringToDouble(Utils.GetDictValue(dict, TRAF_RX_BYTES)));
            TxBytes.Add(Utils.StringToDouble(Utils.GetDictValue(dict, TRAF_TX_BYTES)));
            BandWidth = Utils.StringToDouble(Utils.GetDictValue(dict, TRAF_BANDWIDTH));

            #region RX Traffic data
            RxCrcErrorFrames.Add(Utils.StringToDouble(Utils.GetDictValue(dict, TRAF_CRC_ERROR_FRAMES)));
            RxAlignmentsError.Add(Utils.StringToDouble(Utils.GetDictValue(dict, TRAF_ALIGNEMENTS_ERROR)));
            RxUnicastFrames.Add(Utils.StringToDouble(Utils.GetDictValue(dict, $"{TRAF_RX}{TRAF_UNICAST_FRAMES}")));
            RxBroadcastFrames.Add(Utils.StringToDouble(Utils.GetDictValue(dict, $"{TRAF_RX}{TRAF_BROADCAST_FRAMES}")));
            RxMulticastFrames.Add(Utils.StringToDouble(Utils.GetDictValue(dict, $"{TRAF_RX}{TRAF_MULTICAST_FRAMES}")));
            RxErrorFrames.Add(Utils.StringToDouble(Utils.GetDictValue(dict, $"{TRAF_RX}{TRAF_ERROR_FRAMES}")));
            RxUnderSizeFrames.Add(Utils.StringToDouble(Utils.GetDictValue(dict, $"{TRAF_RX}{TRAF_UNDERSIZE_FRAMES}")));
            RxOverSizeFrames.Add(Utils.StringToDouble(Utils.GetDictValue(dict, $"{TRAF_RX}{TRAF_OVERSIZE_FRAMES}")));
            RxLostFrames.Add(Utils.StringToDouble(Utils.GetDictValue(dict, $"{TRAF_RX}{TRAF_LOST_FRAMES}")));
            #endregion

            #region TX Traffic data
            TxUnicastFrames.Add(Utils.StringToDouble(Utils.GetDictValue(dict, $"{TRAF_TX}{TRAF_UNICAST_FRAMES}")));
            TxBroadcastFrames.Add(Utils.StringToDouble(Utils.GetDictValue(dict, $"{TRAF_TX}{TRAF_BROADCAST_FRAMES}")));
            TxMulticastFrames.Add(Utils.StringToDouble(Utils.GetDictValue(dict, $"{TRAF_TX}{TRAF_MULTICAST_FRAMES}")));
            TxErrorFrames.Add(Utils.StringToDouble(Utils.GetDictValue(dict, $"{TRAF_TX}{TRAF_ERROR_FRAMES}")));
            TxUnderSizeFrames.Add(Utils.StringToDouble(Utils.GetDictValue(dict, $"{TRAF_TX}{TRAF_UNDERSIZE_FRAMES}")));
            TxOverSizeFrames.Add(Utils.StringToDouble(Utils.GetDictValue(dict, $"{TRAF_TX}{TRAF_OVERSIZE_FRAMES}")));
            TxLostFrames.Add(Utils.StringToDouble(Utils.GetDictValue(dict, $"{TRAF_TX}{TRAF_LOST_FRAMES}")));
            TxCollidedFrames.Add(Utils.StringToDouble(Utils.GetDictValue(dict, TRAF_COLLIDED_FRAMES)));
            TxCollisions.Add(Utils.StringToDouble(Utils.GetDictValue(dict, TRAF_COLLISIONS)));
            TxLateCollisions.Add(Utils.StringToDouble(Utils.GetDictValue(dict, TRAF_LATE_COLLISIONS)));
            TxExcCollisions.Add(Utils.StringToDouble(Utils.GetDictValue(dict, TRAF_EXC_COLLISIONS)));
            #endregion

            #region Unused properties
            LinkQuality = Utils.GetDictValue(dict, TRAF_LINK_QUALITY);
            LongFrameSize = Utils.StringToDouble(Utils.GetDictValue(dict, TRAF_LONG_FRAME_SIZE));
            InterFrameGap = Utils.StringToDouble(Utils.GetDictValue(dict, TRAF_INTER_FRAME_GAP));
            #endregion
        }

    }

}
