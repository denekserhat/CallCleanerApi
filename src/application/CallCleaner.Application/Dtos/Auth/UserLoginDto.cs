﻿namespace CallCleaner.Application.Dtos.Auth
{
    public class UserLoginDto
    {
        public required string Mail { get; set; }
        public required string Password { get; set; }
    }
}
