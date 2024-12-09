namespace YangSpaceApp.Server.Data.Services
{
    public class RoleChangeRequestModel
    {
        public string UserId { get; set; }
        public string NewRole { get; set; } // The requested role (e.g., 'ServiceProvider', 'Admin')
        public string Reason { get; set; } // The reason for the role change request
        public DateTime RequestedAt { get; set; } = DateTime.Now;
    }
}
