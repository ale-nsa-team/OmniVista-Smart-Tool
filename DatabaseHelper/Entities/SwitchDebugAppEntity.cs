using System;

namespace DatabaseHelper.Entities
{
    public class SwitchDebugAppEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string AppId { get; set; }
        public string AppIndex { get; set; }
        public string NbSubApp { get; set; }
        public int DebugLevel { get; set; }
        public Guid SwitchId { get; set; }
    }
}