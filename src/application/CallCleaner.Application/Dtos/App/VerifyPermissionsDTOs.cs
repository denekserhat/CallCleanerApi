using System.Collections.Generic;

namespace CallCleaner.Application.Dtos.App
{
    public class VerifyPermissionsRequestDTO
    {
        public List<string> GrantedPermissions { get; set; }
    }

    public class VerifyPermissionsResponseDTO
    {
        public string Status { get; set; } // "ok", "partial", "missing"
        public List<string>? Missing { get; set; }
    }
} 