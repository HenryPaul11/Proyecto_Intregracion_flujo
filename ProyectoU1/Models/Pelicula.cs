using System.ComponentModel.DataAnnotations;

namespace ProyectoU1.Models;

public sealed class Pelicula
{
    public int Id { get; set; }
    [Required(ErrorMessage = "El título es obligatorio."), StringLength(255, ErrorMessage = "El título no puede superar los 255 caracteres.")]
    [Display(Name = "Título")]
    public string Titulo { get; set; } = "";
    [StringLength(5000, ErrorMessage = "La descripción no puede superar los 5000 caracteres."), Display(Name = "Descripción")]
    public string? Descripcion { get; set; }
    [Range(1888, 2100, ErrorMessage = "El año debe estar entre 1888 y 2100."), Display(Name = "Año de estreno")]
    public int? Anio { get; set; }
    [Required(ErrorMessage = "Seleccione un idioma."), Range(1, 255, ErrorMessage = "Seleccione un idioma válido."), Display(Name = "Idioma")]
    public byte IdiomaId { get; set; }
    [Required(ErrorMessage = "Ingrese los días de alquiler."), Range(1, 255, ErrorMessage = "Los días de alquiler deben estar entre 1 y 255."), Display(Name = "Días de alquiler")]
    public byte DuracionAlquiler { get; set; } = 3;
    [Range(typeof(decimal), "0.01", "99.99", ParseLimitsInInvariantCulture = true, ErrorMessage = "La tarifa debe estar entre 0.01 y 99.99."), Required(ErrorMessage = "Ingrese la tarifa de alquiler."), Display(Name = "Tarifa de alquiler")]
    public decimal Tarifa { get; set; } = 4.99m;
    [Range(1, 32767, ErrorMessage = "La duración debe estar entre 1 y 32767 minutos."), Display(Name = "Duración (minutos)")]
    public short? Duracion { get; set; }
    [Range(typeof(decimal), "0.01", "999.99", ParseLimitsInInvariantCulture = true, ErrorMessage = "El costo debe estar entre 0.01 y 999.99."), Required(ErrorMessage = "Ingrese el costo de reposición."), Display(Name = "Costo de reposición")]
    public decimal CostoReposicion { get; set; } = 19.99m;
    [Required(ErrorMessage = "Seleccione una clasificación."), RegularExpression("^(G|PG|PG-13|R|NC-17)$", ErrorMessage = "Seleccione una clasificación válida."), Display(Name = "Clasificación")]
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
public sealed record InventarioModel(Pelicula Pelicula, IReadOnlyList<Copia> Copias, IReadOnlyList<Opcion> Tiendas, int Pagina = 1, bool HayMas = false);
