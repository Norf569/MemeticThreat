using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MemeticThreatClient.Models
{
    interface IDataInstance 
    { 
        string Name { get; set; }
    }
    internal class FileModel : IDataInstance
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("path")]
        public string Path { get; set; }
        [JsonPropertyName("fileName")]
        public string Name { get; set; }
        [JsonPropertyName("fileSize")]
        public long FileSize { get; set; }
        [JsonPropertyName("userId")]
        public int UserId { get; set; }
    }
    internal class Directory : IDataInstance 
    {
        public string Name { get; set; }
        public Directory(string name) { this.Name = name; }
    }
}
