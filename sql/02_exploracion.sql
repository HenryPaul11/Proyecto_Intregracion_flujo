-- Enunciado 1: las 10 películas con mayor cantidad de alquileres.
SELECT TOP (10) f.title AS Pelicula, COUNT(*) AS Alquileres
FROM dbo.film f
INNER JOIN dbo.inventory i ON i.film_id = f.film_id
INNER JOIN dbo.rental r ON r.inventory_id = i.inventory_id
GROUP BY f.film_id, f.title
ORDER BY Alquileres DESC, f.title, f.film_id;

-- Enunciado 2: los 5 clientes con más alquileres.
SELECT TOP (5) c.customer_id, CONCAT(c.first_name, ' ', c.last_name) AS Cliente,
       COUNT(*) AS Alquileres
FROM dbo.customer c
INNER JOIN dbo.rental r ON r.customer_id = c.customer_id
GROUP BY c.customer_id, c.first_name, c.last_name
ORDER BY Alquileres DESC, c.customer_id;

-- Enunciado 3: 10 películas con al menos una copia disponible.
SELECT TOP (10) f.title AS Pelicula, COUNT(*) AS CopiasDisponibles
FROM dbo.film f
INNER JOIN dbo.inventory i ON i.film_id = f.film_id AND i.activo = 1
WHERE f.activo = 1 AND NOT EXISTS (
    SELECT 1 FROM dbo.rental r
    WHERE r.inventory_id = i.inventory_id AND r.return_date IS NULL)
GROUP BY f.film_id, f.title
ORDER BY f.title, f.film_id;
