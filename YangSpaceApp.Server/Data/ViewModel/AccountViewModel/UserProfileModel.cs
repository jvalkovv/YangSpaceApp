using System.ComponentModel.DataAnnotations;

namespace YangSpaceApp.Server.Data.ViewModel.AccountViewModel
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

        public string Bio { get; set; }

        public string? ProfilePictureUrl { get; set; }

        public IFormFile ProfilePicture { get; set; }

        public string? Role { get; set; }
        public string? Location { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }
    }
}
