using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OsFacil.Models
{
    [Table("OS_USUARIOS")]
    public class Usuario
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required(ErrorMessage = "Nome é obrigatório")]
        [MaxLength(100, ErrorMessage = "O nome deve conter no máximo 100 caracteres")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage ="Email é obrigatório")]
        [EmailAddress(ErrorMessage ="Email inválido")]
        [MaxLength(100, ErrorMessage ="O email deve conter no máximo 100 caracteres")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage ="Password é obrigatório")]
        [MinLength(6, ErrorMessage ="A senha deve conter no mínimo 6 caracteres")]
        [MaxLength(15, ErrorMessage ="A senha deve conter no máximo 15 caracteres")]
        public string PasswordHash { get; set; } = string.Empty;


        public ICollection<Carro> Carros { get; set; } = new List<Carro>();

        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    }
}
