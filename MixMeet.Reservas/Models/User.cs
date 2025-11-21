using System.ComponentModel.DataAnnotations;

namespace MixMeet.Reservas.Models
{
    public class User
    {
        // O número de telefone será a chave primária, pois é único e validado
        [Key]
        public string PhoneNumber { get; set; }

        [Required]
        [StringLength(50)]
        public string Nickname { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}