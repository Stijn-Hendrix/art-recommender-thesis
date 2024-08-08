using System.ComponentModel.DataAnnotations;

namespace frontend.Models
{
    public class Artwork
    {
        [Key]
        public string ImagePath { get; set; }
        public string Artstyle { get; set; }
        public string Theme { get; set; }
        public List<string> Colors { get; set; }
        public List<string> Objects { get; set; }
        public List<string> Semantics { get; set; }
        public string Description { get; set; }
    }
}
