using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace PoEWizard.Device
{
    public class SnmpModel : ICloneable
    {
        public ObservableCollection<SnmpUser> Users { get; set; }
        public ObservableCollection<SnmpCommunity> Communities { get; set; }
        public ObservableCollection<SnmpStation> Stations { get; set; }

        public SnmpModel() 
        {
            Users = new ObservableCollection<SnmpUser>();
            Communities = new ObservableCollection<SnmpCommunity>();
            Stations = new ObservableCollection<SnmpStation>();
        }

        public void AddUser(string name, string password)
        {
            Users.Add(new SnmpUser(name)
            {
                Password = password,
                PrivateKey = password,
                Protocol = "MD5",
                Encryption = "DES"
            });
        }

        public void AddCommunity(string name, string user)
        {
            Communities.Add(new SnmpCommunity(name, user, null));
        }

        public void AddStation(string ipAddress, string version, string user, string community)
        {
            string ip_port = $"{ipAddress}/162";
            Stations.Add(new SnmpStation(ip_port, null, version, user, community));
        }

        public object Clone()
        {
            SnmpModel clone = new SnmpModel();
            clone.Users = new ObservableCollection<SnmpUser>();
            clone.Communities = new ObservableCollection<SnmpCommunity>();
            clone.Stations = new ObservableCollection<SnmpStation>();
            foreach (SnmpUser user in Users) clone.Users.Add(user.Clone() as SnmpUser);
            foreach(SnmpCommunity community in Communities) clone.Communities.Add(community.Clone() as SnmpCommunity);
            foreach (SnmpStation station in Stations) clone.Stations.Add(station.Clone() as SnmpStation);
            return clone;
        }
    }
}
