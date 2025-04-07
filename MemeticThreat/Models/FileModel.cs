using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MemeticThreatServerAPI.Models
{
    public class FileModel
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Path { get; set; } = null!;
        [Required]
        public string FileName { get; set; } = null!;
        [Required]
        public long FileSize { get; set; }
        [Required]
        public int UserId { get; set; }
        [JsonIgnore]
        public User? User { get; set; }
    }
}