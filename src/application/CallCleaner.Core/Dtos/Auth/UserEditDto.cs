﻿namespace CallCleaner.Core.Dtos.Auth
{
    public class UserEditDto
    {
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public string? District { get; set; }
        public string? City { get; set; }
        public string? ImageUrl { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
    }
}
