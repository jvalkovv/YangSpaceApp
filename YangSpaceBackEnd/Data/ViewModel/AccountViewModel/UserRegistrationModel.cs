using System.ComponentModel.DataAnnotations;

namespace YangSpaceBackEnd.Data.ViewModel.AccountViewModel
{
    public class UserRegistrationModel
    {
        [Required]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public bool IsServiceProvider { get; set; }

    }
}
