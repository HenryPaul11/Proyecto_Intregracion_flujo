using System.Data;
using Microsoft.Data.SqlClient;
using ProyectoU1.Models;

namespace ProyectoU1.Data;

public sealed class UsuarioRepository(SqlConnectionFactory factory)
{
    public async Task<Usuario?> FindAsync(string login)
    {
        await using var connection = await factory.OpenAsync();
        await using var command = new SqlCommand("""
            SELECT u.UsuarioId, u.Nombre, u.Login, u.PasswordHash, u.Rol, u.CustomerId, u.StaffId
            FROM dbo.Usuarios u
            LEFT JOIN dbo.customer c ON c.customer_id=u.CustomerId
            LEFT JOIN dbo.staff s ON s.staff_id=u.StaffId
            WHERE u.Login=@login AND u.Activo=1
              AND ((u.Rol='Cliente' AND c.active IN ('1','Y')) OR (u.Rol='Administrador' AND s.active=1))
            """, connection);
        command.Parameters.Add("@login", SqlDbType.NVarChar, 80).Value = login;
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return new Usuario
        {
            Id = reader.GetInt32(0),
            Nombre = reader.GetString(1),
            Login = reader.GetString(2),
            PasswordHash = reader.GetString(3),
            Rol = reader.GetString(4),
            CustomerId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
            StaffId = reader.IsDBNull(6) ? null : reader.GetByte(6)
        };
    }

    public async Task UpdateHashAsync(int id, string hash)
    {
        await using var connection = await factory.OpenAsync();
        await using var command = new SqlCommand("UPDATE dbo.Usuarios SET PasswordHash=@hash WHERE UsuarioId=@id", connection);
        command.Parameters.Add("@hash", SqlDbType.NVarChar, 512).Value = hash;
        command.Parameters.Add("@id", SqlDbType.Int).Value = id;
        await command.ExecuteNonQueryAsync();
    }
}
