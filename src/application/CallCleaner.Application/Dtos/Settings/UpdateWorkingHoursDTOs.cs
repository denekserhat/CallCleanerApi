using CallCleaner.Application.Dtos.Core;

namespace CallCleaner.Application.Dtos.Settings
{
    public class UpdateWorkingHoursRequestDTO : WorkingHoursDTO // Reuse WorkingHoursDTO
    {
    }

    public class UpdateWorkingHoursResponseDTO
    {
        public string Message { get; set; }
    }
} 