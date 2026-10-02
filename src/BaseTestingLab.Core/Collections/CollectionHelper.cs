using System.Reflection;
using BaseTestingLab.Core.Common.Exceptions;

namespace BaseTestingLab.Core.Collections;

public static class CollectionHelper
{
    public static PagedResult<T> ToPagedResult<T>(IEnumerable<T> source, int pageNumber, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (pageNumber < 1)
            throw new ValidationException("El número de página debe ser mayor o igual a 1.", nameof(pageNumber));

        if (pageSize < 1)
            throw new ValidationException("El tamaño de página debe ser mayor o igual a 1.", nameof(pageSize));

        var list = source as IReadOnlyList<T> ?? source.ToList();
        var totalItems = list.Count;

        var items = list
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<T>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public static IEnumerable<List<T>> ChunkBySize<T>(IEnumerable<T> source, int chunkSize)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (chunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(chunkSize), "El tamaño del fragmento debe ser mayor a 0.");

        return source.Chunk(chunkSize).Select(c => c.ToList());
    }

    public static IEnumerable<T> SortByProperty<T>(IEnumerable<T> source, string propertyName, bool ascending = true)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (string.IsNullOrWhiteSpace(propertyName))
            throw new ArgumentException("El nombre de la propiedad no puede estar vacío.", nameof(propertyName));

        var prop = typeof(T).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        if (prop == null)
            throw new ArgumentException($"La propiedad '{propertyName}' no existe en el tipo '{typeof(T).Name}'.", nameof(propertyName));

        return ascending
            ? source.OrderBy(x => prop.GetValue(x, null))
            : source.OrderByDescending(x => prop.GetValue(x, null));
    }

    public static IEnumerable<TKey> FindDuplicates<T, TKey>(IEnumerable<T> source, Func<T, TKey> keySelector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

        return source
            .GroupBy(keySelector)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
    }
}
