namespace CallCleaner.Application.Dtos.Auth
{
    public class UserRegisterDto
    {
        //[Required(ErrorMessage ="Ad alanı zorunludur")]
        //[Display(Name="İsim:")]
        //[MaxLength(30,ErrorMessage ="En fazla 30 karakter girebilirsiniz")]
        public string Name { get; set; }
        public string Surname { get; set; }
        public string? Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }

    public class UserRegisterResponseDto
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; }
    }
}
