using System.ComponentModel.DataAnnotations;

public class UpdateUserDTO
{
    
    public bool ModifyEmail { get; set; }

   
    public bool ModifyPassword { get; set; }

    [EmailAddress(ErrorMessage = "Érvénytelen email cím.")]
    public string? NewEmail { get; set; }

    public string? NewPassword { get; set; }
}
