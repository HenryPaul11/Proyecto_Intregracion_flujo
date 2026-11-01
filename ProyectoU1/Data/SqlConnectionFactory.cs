using Microsoft.Data.SqlClient;

namespace ProyectoU1.Data;

public sealed class SqlConnectionFactory(IConfiguration configuration)
{
    public async Task<SqlConnection> OpenAsync()
    {
        var connection = new SqlConnection(configuration.GetConnectionString("Sakila")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:Sakila."));
        try
        {
            await connection.OpenAsync();
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
