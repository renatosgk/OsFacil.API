using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OsFacil.Models
{
    [Table("OS_FUNCIONARIOS")]
    public class Funcionario
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required(ErrorMessage = "O nome do funcionário é obrigatório")]
        [MaxLength(100, ErrorMessage = "O nome do funcionário deve conter no máximo 100 caracteres")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "O cargo do funcionário é obrigatório")]
        [MaxLength(50, ErrorMessage = "O cargo do funcionário deve conter no máximo 50 caracteres")]
        public string Cargo { get; set; } = string.Empty;

        [Required(ErrorMessage = "O salário do funcionário é obrigatório")]
        public decimal Salario { get; set; }
    
        public DateTime DataAdmissao { get; set; } = DateTime.UtcNow;
    }
}
