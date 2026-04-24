using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using AspNetTemplate.Core.Data.Base;
using AspNetTemplate.Core.Data.DbContexts;

namespace AspNetTemplate.Core.Infra.Extensions;

public static class IQueryableExtensions
{
    // TODO: add ApplyBaseFilter<T>(this IQueryable<T> query, BaseFilter filter) that applies
    // Id, CreatedAfter, CreatedBefore in one call — shorthand for filter.ApplyFilters(query)
    // when you can't call ApplyFilters directly (e.g. when working with a projected IQueryable).

    public static async Task<PagedList<T>> Paginate<T>(this IQueryable<T> query, BaseFilter filter, CancellationToken ct)
    {
        if (query == null) return null!;

        return await PagedList<T>.Create(query, filter.PageNumber, filter.PageSize, ct);
    }

    public static IQueryable<T> WhereSoftDeleted<T>(this IQueryable<T> query, bool? filterValue)
        where T : BaseSoftEntity
    {
        if (filterValue == null)
            return query;

        return query.Where(e => e.IsDeleted == filterValue);
    }

    public static IQueryable<T> WhereIf<T>(this IQueryable<T> query,
        Expression<Func<T, bool>> predicate,
        bool condition)
    {
        return condition ? query.Where(predicate) : query;
    }

    public static IQueryable<T> OrderBy<T, TKey>(
        this IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector,
        bool ascending = true)
    {
        return ascending ? query.OrderBy(keySelector) : query.OrderByDescending(keySelector);
    }

    public static IQueryable<T> WhereLike<T>(
        this IQueryable<T> query,
        Expression<Func<T, string?>> propertySelector,
        string? searchText) where T : BaseEntity
    {
        if (string.IsNullOrEmpty(searchText))
            return query;

        var parameter = Expression.Parameter(typeof(T), "e");

        // Convert property to lowercase
        var property = Expression.Invoke(propertySelector, parameter);
        var toLowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes);
        var propertyLower = Expression.Call(property, toLowerMethod!);

        // Lowercase search pattern
        var searchPattern = Expression.Constant($"%{searchText.ToLower()}%");

        // Build EF.Functions.Like call
        var likeMethod = typeof(DbFunctionsExtensions).GetMethod(
            nameof(DbFunctionsExtensions.Like),
            [typeof(DbFunctions), typeof(string), typeof(string)]);

        var efFunctions = Expression.Property(null, typeof(EF), nameof(EF.Functions));
        var likeCall = Expression.Call(likeMethod!, efFunctions, propertyLower, searchPattern);

        var lambda = Expression.Lambda<Func<T, bool>>(likeCall, parameter);

        return query.Where(lambda);
    }

    public static IQueryable<T> WhereDateRange<T>(
        this IQueryable<T> query,
        DateTime? startDate,
        DateTime? endDate) where T : BaseSoftEntity
    {
        return query.WhereBetween(startDate, endDate, e => e.CreatedAt);
    }

    public static IQueryable<T> WhereBetween<T, TProperty>(
        this IQueryable<T> query,
        TProperty? start,
        TProperty? end,
        Expression<Func<T, TProperty>> propertySelector)
        where TProperty : struct, IComparable<TProperty>
    {
        if (start.HasValue)
        {
            query = query.Where(Expression.Lambda<Func<T, bool>>(
                Expression.GreaterThanOrEqual(propertySelector.Body, Expression.Constant(start.Value)),
                propertySelector.Parameters));
        }

        if (end.HasValue)
        {
            query = query.Where(Expression.Lambda<Func<T, bool>>(
                Expression.LessThanOrEqual(propertySelector.Body, Expression.Constant(end.Value)),
                propertySelector.Parameters));
        }

        return query;
    }

    public static IQueryable<T> WhereBetween<T, TProperty>(
        this IQueryable<T> query,
        TProperty? start,
        TProperty? end,
        Expression<Func<T, TProperty?>> propertySelector)
        where TProperty : struct, IComparable<TProperty>
    {
        var param = propertySelector.Parameters[0];

        if (start.HasValue)
        {
            var value = Expression.Property(propertySelector.Body, "Value");
            var hasValue = Expression.Property(propertySelector.Body, "HasValue");
            var condition = Expression.AndAlso(
                hasValue,
                Expression.GreaterThanOrEqual(value, Expression.Constant(start.Value))
            );
            query = query.Where(Expression.Lambda<Func<T, bool>>(condition, param));
        }

        if (end.HasValue)
        {
            var value = Expression.Property(propertySelector.Body, "Value");
            var hasValue = Expression.Property(propertySelector.Body, "HasValue");
            var condition = Expression.AndAlso(
                hasValue,
                Expression.LessThanOrEqual(value, Expression.Constant(end.Value))
            );
            query = query.Where(Expression.Lambda<Func<T, bool>>(condition, param));
        }

        return query;
    }

    public static IQueryable<T> OrderByCreationDate<T>(this IQueryable<T> query) where T : BaseSoftEntity
    {
        return query.OrderByDescending(e => e.CreatedAt);
    }

    public static IQueryable<T> WhereFilter<T, TFilter>(this IQueryable<T> query, TFilter filter,
        List<string>? exclude = null)
    {
        if (filter == null) return query;

        // Start with a default expression that always evaluates to true
        Expression<Func<T, bool>> predicate = _ => true;

        // Iterate through all properties of the filter and create corresponding expressions
        var filterProperties = filter.GetType().GetProperties();

        foreach (var filterProperty in filterProperties)
        {
            if (exclude?.Contains(filterProperty.Name) == true) continue;
            // Only apply the filter if the property is not null or has a valid value
            var filterValue = filterProperty.GetValue(filter);
            if (filterValue != null)
            {
                // Try to find the corresponding property in the entity (T)
                var entityProperty = typeof(T).GetProperty(filterProperty.Name);

                if (entityProperty != null)
                {
                    // Dynamically build the filter expression based on property types
                    var parameter = Expression.Parameter(typeof(T), "e");
                    var entityPropertyAccess = Expression.Property(parameter, entityProperty);

                    // Create a comparison expression based on the property type
                    Expression? comparison = null;

                    if (filterValue is string stringValue)
                    {
                        // For strings, we can use `Contains`
                        comparison = Expression.Call(entityPropertyAccess, "Contains", null,
                            Expression.Constant(stringValue));
                    }
                    else if (filterValue is bool boolValue)
                    {
                        // For booleans, we just check for equality
                        comparison = Expression.Equal(entityPropertyAccess, Expression.Constant(boolValue));
                    }
                    else if (filterValue is DateTime dateTimeValue)
                    {
                        // For DateTime, check for equality
                        comparison = Expression.Equal(entityPropertyAccess, Expression.Constant(dateTimeValue));
                    }
                    else if (filterValue is int intValue)
                    {
                        // For integers, check for equality
                        comparison = Expression.Equal(entityPropertyAccess, Expression.Constant(intValue));
                    }
                    else
                    {
                        // For other types (e.g., Enum, Nullable types), you can handle them as needed
                        comparison = Expression.Equal(entityPropertyAccess, Expression.Constant(filterValue));
                    }

                    // Combine the current predicate with the new condition
                    predicate = predicate.AndAlso(Expression.Lambda<Func<T, bool>>(comparison, parameter));
                }
            }
        }

        // Apply the dynamically built filter to the query
        return query.Where(predicate);
    }

    // Helper function for combining expressions with AND
    public static Expression<Func<T, bool>> AndAlso<T>(this Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        var parameter = Expression.Parameter(typeof(T), "e");
        var combined = Expression.AndAlso(
            Expression.Invoke(expr1, parameter),
            Expression.Invoke(expr2, parameter)
        );
        return Expression.Lambda<Func<T, bool>>(combined, parameter);
    }
}