using System;
using System.Collections.Generic;
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

        public void DeleteUser(SnmpUser user)
        {
            Users.Remove(user);
        }

        public void AddCommunity(string name, string user)
        {
            Communities.Add(new SnmpCommunity(name, user, null));
        }

        public void DeleteCommunity(SnmpCommunity community)
        {
            Communities.Remove(community);
        }

        public void AddStation(string ipAddress, string version, string user, string community)
        {
            string ip_port = $"{ipAddress}/162";
            Stations.Add(new SnmpStation(ip_port, null, version, user, community));
        }

        public void DeleteStation(SnmpStation station)
        {
            Stations.Remove(station);
        }

        public List<SnmpCommunity> GetUserCommunities(string user)
        {
            return Communities.ToList().FindAll(c => c.User == user);
        }

        public List<SnmpStation> GetUserStations(string user)
        {
            List<SnmpStation> list = new List<SnmpStation>();

            list.AddRange(Stations.ToList().FindAll(s => s.User == user));
            List<SnmpCommunity> cms = GetUserCommunities(user);
            list.AddRange(Stations.ToList().FindAll(s => cms.Find(c => c.Name == s.Community) != null));
            return list;
        }

        public List<SnmpStation> GetCommunityStations(string community)
        {
            return Stations.ToList().FindAll(s => s.Community == community);
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

        public bool HasChanges(SnmpModel orig)
        {
            bool usr = this.Users.Count != orig.Users.Count;
            bool comm = this.Communities.Count != orig.Communities.Count;
            bool st = this.Stations.Count != orig.Stations.Count;
            return usr || comm || st;
        }
    }
}
