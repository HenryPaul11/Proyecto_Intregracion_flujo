using System.ComponentModel.DataAnnotations;

namespace ProyectoU1.Models;

public sealed class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Login { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Rol { get; set; } = "";
    public int? CustomerId { get; set; }
    public byte? StaffId { get; set; }
}

public sealed class LoginModel
{
    [Required(ErrorMessage = "Ingrese su usuario."), StringLength(80)]
    [Display(Name = "Usuario")]
    public string Login { get; set; } = "";

    [Required(ErrorMessage = "Ingrese su contraseña."), StringLength(128), DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = "";
}
