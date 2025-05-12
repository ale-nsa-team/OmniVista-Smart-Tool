using System;

namespace DatabaseHelper.Entities
{
    public class DatabaseVersionEntity
    {
        public Guid Id { get; set; }
        public string Version { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string Description { get; set; }
    }
}