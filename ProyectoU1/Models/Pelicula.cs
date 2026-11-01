using System.ComponentModel.DataAnnotations;

namespace ProyectoU1.Models;

public sealed class Pelicula
{
    public int Id { get; set; }
    [Required(ErrorMessage = "El título es obligatorio."), StringLength(255)]
    [Display(Name = "Título")]
    public string Titulo { get; set; } = "";
    [StringLength(5000), Display(Name = "Descripción")]
    public string? Descripcion { get; set; }
    [Range(1888, 2100), Display(Name = "Año de estreno")]
    public int? Anio { get; set; }
    [Range(1, 255), Display(Name = "Idioma")]
    public byte IdiomaId { get; set; }
    [Range(1, 255), Display(Name = "Días de alquiler")]
    public byte DuracionAlquiler { get; set; } = 3;
    [Range(typeof(decimal), "0.01", "99.99", ParseLimitsInInvariantCulture = true), Display(Name = "Tarifa de alquiler")]
    public decimal Tarifa { get; set; } = 4.99m;
    [Range(1, 32767), Display(Name = "Duración (minutos)")]
    public short? Duracion { get; set; }
    [Range(typeof(decimal), "0.01", "999.99", ParseLimitsInInvariantCulture = true), Display(Name = "Costo de reposición")]
    public decimal CostoReposicion { get; set; } = 19.99m;
    [Required, RegularExpression("^(G|PG|PG-13|R|NC-17)$"), Display(Name = "Clasificación")]
    public string Clasificacion { get; set; } = "G";
    public bool Activo { get; set; } = true;
    public int Copias { get; set; }
    public int Disponibles { get; set; }
}

public sealed record Opcion(int Id, string Nombre);
public sealed record Copia(int Id, int TiendaId, bool Activa, bool Alquilada);
public sealed record CatalogoModel(IReadOnlyList<Pelicula> Peliculas, string? Buscar, int Pagina, int Total, bool Administracion)
{
    public int Paginas => Math.Max(1, (int)Math.Ceiling(Total / 24.0));
}
public sealed record InventarioModel(Pelicula Pelicula, IReadOnlyList<Copia> Copias, IReadOnlyList<Opcion> Tiendas);
