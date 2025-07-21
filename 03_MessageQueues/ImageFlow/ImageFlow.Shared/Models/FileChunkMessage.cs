using System.ComponentModel.DataAnnotations;

namespace ImageFlow.Shared.Models
{
    public class FileChunkMessage
    {
        [Required]
        public string FileId { get; set; } = string.Empty;
        
        [Required]
        public string FileName { get; set; } = string.Empty;
        
        [Range(1, int.MaxValue)]
        public int ChunkNumber { get; set; }
        
        [Range(1, int.MaxValue)]
        public int TotalChunks { get; set; }
        
        [Required]
        public string FileType { get; set; } = string.Empty;
        
        [Required]
        public byte[] ChunkData { get; set; } = Array.Empty<byte>();
        
        public long OriginalFileSize { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string ChecksumMd5 { get; set; } = string.Empty;
    }
} 