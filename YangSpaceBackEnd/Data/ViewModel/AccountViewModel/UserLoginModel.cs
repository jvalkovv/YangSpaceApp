using System.ComponentModel.DataAnnotations;

namespace YangSpaceBackEnd.Data.ViewModel.AccountViewModel
{
    public class UserLoginModel
    {
        [Required]
        [StringLength(20, MinimumLength = 3)]
        public string? Username { get; set; }

        [Required]
        [StringLength(36, MinimumLength = 6)]
        public string? Password { get; set; }
    }
}
