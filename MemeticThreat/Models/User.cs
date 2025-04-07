using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MemeticThreatServerAPI.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = null!;
        [Required]
        public string Email { get; set; } = null!;
        [Required]
        [JsonIgnore]
        public string Password { get; set; } = null!;
        public long TotalFileSize { get; set; } = 0;
        public long StorageSpace { get; set; } = (long)10737418240; //10 * 1024 * 1024 * 1024
        [JsonIgnore]
        public List<FileModel> FileModels { get; set; } = new();
    }
}
