namespace NKZAPI.Dtos
{
    using System.ComponentModel.DataAnnotations;

    public class WalletDepositDto
    {
        [Range(typeof(decimal), "5", "5000", ErrorMessage = "A recarga deve ficar entre R$ 5,00 e R$ 5.000,00.")]
        public decimal Amount { get; set; }
    }
}
