using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using ProyectoU1.Models;

namespace ProyectoU1.Data;

public static class UserSetup
{
    // Uso local: --crear-usuario login Nombre Cliente|Administrador idSakila
    public static async Task RunAsync(IServiceProvider services, string[] args)
    {
        var position = Array.IndexOf(args, "--crear-usuario");
        if (position < 0 || args.Length != position + 5)
            throw new ArgumentException("Uso: --crear-usuario login Nombre Cliente|Administrador idSakila");
        var login = args[position + 1];
        var name = args[position + 2];
        var role = args[position + 3];
        if (string.IsNullOrWhiteSpace(login) || login.Length > 80 || string.IsNullOrWhiteSpace(name) || name.Length > 100
            || role is not ("Cliente" or "Administrador") || !int.TryParse(args[position + 4], out var id) || id < 1)
            throw new ArgumentException("Datos de usuario inválidos.");
        var password = Environment.GetEnvironmentVariable("SAKILA_SETUP_PASSWORD")
            ?? throw new ArgumentException("Defina SAKILA_SETUP_PASSWORD solo para este proceso.");
        if (password.Length < 12 || password.Length > 128)
            throw new ArgumentException("La contraseña debe tener de 12 a 128 caracteres.");
        using var scope = services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<SqlConnectionFactory>();
        var user = new Usuario { Login = login, Nombre = name, Rol = role };
        var hash = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>().HashPassword(user, password);
        await using var connection = await factory.OpenAsync();
        await using var command = new SqlCommand("""
            INSERT INTO dbo.Usuarios(Nombre, Login, PasswordHash, Rol, CustomerId, StaffId)
            VALUES (@name, @login, @hash, @role, @customer, @staff)
            """, connection);
        command.Parameters.Add("@name", SqlDbType.NVarChar, 100).Value = name;
        command.Parameters.Add("@login", SqlDbType.NVarChar, 80).Value = login;
        command.Parameters.Add("@hash", SqlDbType.NVarChar, 512).Value = hash;
        command.Parameters.Add("@role", SqlDbType.VarChar, 20).Value = role;
        command.Parameters.Add("@customer", SqlDbType.Int).Value = role == "Cliente" ? id : DBNull.Value;
        command.Parameters.Add("@staff", SqlDbType.TinyInt).Value = role == "Administrador" ? id : DBNull.Value;
        await command.ExecuteNonQueryAsync();
        Console.WriteLine("Usuario creado con contraseña protegida.");
    }
}
