using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OsFacil.Models
{
    [Table("OS_ITEMSERVICO")]
    public class ItemServico
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required(ErrorMessage = "A descrição do serviço é obrigatória")]
        [MaxLength(200, ErrorMessage = "A descrição do serviço deve conter no máximo 200 caracteres")]
        public string Descricao { get; set; } = string.Empty;

        [Required(ErrorMessage = "O preço unitário do serviço é obrigatório")]
        public decimal PrecoUnitario { get; set; }

        [Required(ErrorMessage = "A quantidade do serviço é obrigatória")]
        public decimal Quantidade { get; set; }

        [ForeignKey("OrdemServicoId")]
        public long OrdemServicoId { get; set; }
         
        public OrdemServico OrdemServico { get; set; }
    }
}
