using OsFacil.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OsFacil.Models
{
    [Table("OS_ORDEM_SERVICO")]
    public class OrdemServico
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required(ErrorMessage = "A descrição é obrigatório")]
        public string Descricao { get; set; } = string.Empty;

        [Required(ErrorMessage = "O valor é obrigatório")]
        public decimal Valor { get; set; }

        [Required(ErrorMessage = "O status é obrigatório")]
        public StatusOS Status { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "O usuário é obrigatório")]
        [ForeignKey("Usuario")]
        public long UsuarioId { get; set; }
        public Usuario Usuario { get; set; }

        [Required(ErrorMessage = "O funcionário é obrigatório")]
        [ForeignKey("Funcionario")]
        public long FuncionarioId { get; set; }
        public Funcionario Funcionario { get; set; }

        [Required(ErrorMessage = "O veículo é obrigatório")]
        [ForeignKey("Carro")]
        public long CarroId { get; set; }
        public Carro Carro { get; set; }

        public ICollection<ItemServico> ItensServico { get; set; } = new List<ItemServico>();
    }
}
