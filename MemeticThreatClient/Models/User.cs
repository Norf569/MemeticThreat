using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MemeticThreatClient.Models
{
    internal class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public long TotalFileSize { get; set; }
        public long StorageSpace { get; set; }
        public string Jwt { get; set; }
    }
}
