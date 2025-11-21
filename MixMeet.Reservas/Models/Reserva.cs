using System;
using System.ComponentModel.DataAnnotations;

namespace MixMeet.Reservas.Models
{
    public class Reserva
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Local { get; set; }
        [Required]
        public string Sala { get; set; }
        [Required]
        public DateTime DataHoraInicio { get; set; }
        [Required]
        public DateTime DataHoraFim { get; set; }
        [Required]
        public string Responsavel { get; set; }
        public bool TemCafe { get; set; } = false;
        [StringLength(255)]
        public string DescricaoCafe { get; set; } 
        public int? QuantidadeCafe { get; set; }
    }
}