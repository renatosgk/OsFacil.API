using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OsFacil.Models
{

    [Table("OS_CARROS")]
    public class Carro
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required(ErrorMessage ="O nome da marca do carro é obrigatório")]
        [MaxLength(50)]
        public string Marca { get; set; } = string.Empty; 

        [Required(ErrorMessage ="O nome do modelo é obrigatório")]
        [MaxLength(50)]
        public string Modelo { get; set; } = string.Empty; 

        [Required(ErrorMessage ="O ano do Carro é obrigatório")]
        public int Ano { get; set; }

        [Required(ErrorMessage ="A placa do Carro é obrigatório")]
        [MaxLength(10)]
        [RegularExpression(@"^[A-Z]{3}\d[A-Z\d]\d{2}$")]
        public string Placa { get; set; } = string.Empty;

        [Required(ErrorMessage = "Cliente obrigatório")]
        [ForeignKey("Usuario")]
        public long UsuarioId { get; set; }
        public Usuario Usuario { get; set; }
    }
}
