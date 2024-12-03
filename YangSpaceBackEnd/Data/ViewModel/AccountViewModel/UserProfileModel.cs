using System.ComponentModel.DataAnnotations;

namespace YangSpaceBackEnd.Data.ViewModel.AccountViewModel
{
    public class UserProfileModel
    {

        [MaxLength(10)]
        public string? UserName { get; set; }

        [MaxLength(50)]
        public string? FirstName { get; set; }

        [MaxLength(50)]
        public string? LastName { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public string? Role { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }
    }
}
