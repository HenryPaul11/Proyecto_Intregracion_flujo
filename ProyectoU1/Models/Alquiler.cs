namespace ProyectoU1.Models;

public sealed record Alquiler(int Id, int CopiaId, string Pelicula, string Cliente, DateTime Fecha,
    DateTime? Devolucion, int Dias)
{
    public DateTime Vencimiento => Fecha.AddDays(Dias);
}
public sealed record AlquileresModel(IReadOnlyList<Alquiler> Alquileres, bool Administracion, int Pagina, bool HayMas);
public sealed record PeliculaReporte(string Titulo, int Cantidad);
