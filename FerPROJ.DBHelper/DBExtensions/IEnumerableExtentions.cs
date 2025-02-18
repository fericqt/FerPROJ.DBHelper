﻿using FerPROJ.DBHelper.DBCache;
using FerPROJ.Design.Class;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FerPROJ.DBHelper.DBExtensions {
    public static class IEnumerableExtentions {

        #region Search By Date
        public static IEnumerable<T> SearchDateRange<T>(this IEnumerable<T> queryable, DateTime? dateFrom, DateTime? dateTo, string dateProperty = "") {
            if (!dateFrom.HasValue && !dateTo.HasValue) {
                return queryable; // Return the original collection if both dateFrom and dateTo are null.
            }

            // Get the specified DateTime property if dateProperty is provided
            PropertyInfo property = null;
            if (!string.IsNullOrEmpty(dateProperty)) {
                property = typeof(T).GetProperty(dateProperty, BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                if (property == null) {
                    throw new ArgumentException($"Property '{dateProperty}' is not found or is not of type DateTime.");
                }
            }
            else {
                // Get all DateTime properties of the type
                var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                                          .Where(p => p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?))
                                          .ToList();

                if (!properties.Any()) {
                    return queryable; // Return the original collection if there are no DateTime properties.
                }
            }

            // Filter the collection
            return queryable.Where(item => {

                var propertiesToCheck = property != null ? new List<PropertyInfo> { property } :
                                        typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                                                 .Where(p => p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?))
                                                 .ToList();

                foreach (var prop in propertiesToCheck) {

                    var propertyValue = (DateTime?)prop.GetValue(item);

                    // If the value is null, skip this property
                    if (!propertyValue.HasValue)
                        continue;

                    // Check if the property is within the date range
                    bool isAfterStart = !dateFrom.HasValue || propertyValue > dateFrom.Value.AddDays(-1);
                    bool isBeforeEnd = !dateTo.HasValue || propertyValue < dateTo.Value.AddDays(1);

                    if (isAfterStart && isBeforeEnd) {
                        return true; // Property is within range
                    }

                }

                return false; // No properties matched the date range
            });
        }
        #endregion

        #region Search By Text
        public static IEnumerable<T> SearchTextByProperty<T>(this IEnumerable<T> queryable, string searchText, string propertyName) {

            // Get the property from the type or its base classes
            PropertyInfo property = null;
            Type type = typeof(T);

            // Traverse up the inheritance chain to find the property in the class or its base classes
            while (type != null) {
                property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (property != null) {
                    break;
                }
                type = type.BaseType;
            }

            // If the property was not found, return the original collection as there's nothing to search by
            if (property == null) {
                throw new ArgumentException($"Property '{propertyName}' not found on type '{typeof(T).Name}' or its base classes.");
            }

            // Check if the found property is of type string
            if (property.PropertyType != typeof(string)) {
                throw new ArgumentException($"Property '{propertyName}' must be of type 'string'.");
            }

            // Build the predicate expression to filter by the search text
            Func<T, bool> predicate = x => {
                var propertyValue = property.GetValue(x) as string;
                return propertyValue != null && propertyValue.SearchFor(searchText);
            };

            // Apply the predicate to filter the collection
            return queryable.Where(predicate);
        }
        public static IEnumerable<T> SearchText<T>(this IEnumerable<T> queryable, string searchText) {
            if (string.IsNullOrEmpty(searchText)) {
                return queryable; // Return original query if searchText is empty.
            }

            // Get the properties of the type and all its base classes
            var properties = new List<PropertyInfo>();
            Type type = typeof(T);

            // Traverse up the inheritance hierarchy to get all string properties
            while (type != null) {
                properties.AddRange(
                    type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                        .Where(p => p.PropertyType == typeof(string))
                );
                type = type.BaseType;
            }

            if (!properties.Any()) {
                return queryable; // Return original query if no string properties are found.
            }

            // Build the predicate expression
            Func<T, bool> predicate = x => {
                // Loop through all string properties and check if any of them contain the search text
                foreach (var property in properties) {
                    // Ignore properties with index parameters to avoid TargetParameterCountException
                    if (property.GetIndexParameters().Length == 0) // Only proceed if there are no index parameters
                    {
                        // Safely retrieve the property value as a string
                        if (property.GetValue(x) is string propertyValue && propertyValue.SearchFor(searchText)) {
                            return true; // Found a match, no need to check other properties
                        }
                    }
                }

                return false; // No match found for any property
            };

            // Apply the predicate to filter the collection
            return queryable.Where(predicate);
        }
        #endregion

        #region Get Active Only
        public static IEnumerable<T> GetAllActiveOnly<T>(this IEnumerable<T> queryable) {
            // Get the Status property if it exists
            var statusProperty = typeof(T).GetProperty("Status");

            // If Status property exists, filter based on active status
            if (statusProperty != null) {
                // Define the predicate to filter active entities
                Func<T, bool> predicate = x => {
                    var statusValue = statusProperty.GetValue(x);

                    // Check if Status property is of type string and matches active status
                    if (statusValue is string statusString && statusString == CStaticVariable.ACTIVE_STATUS) {
                        return true;
                    }

                    return false;
                };

                // Apply the predicate to filter the collection
                return queryable.Where(predicate);
            }

            // If no Status property, return the original collection unfiltered
            return queryable;
        }
        #endregion

        #region Get InActive Only
        public static IEnumerable<T> GetAllInActiveOnly<T>(this IEnumerable<T> queryable) {
            // Get the Status property if it exists
            var statusProperty = typeof(T).GetProperty("Status");

            // If Status property exists, filter based on active status
            if (statusProperty != null) {
                // Define the predicate to filter active entities
                Func<T, bool> predicate = x => {
                    var statusValue = statusProperty.GetValue(x);

                    // Check if Status property is of type string and matches active status
                    if (statusValue is string statusString && statusString == CStaticVariable.IN_ACTIVE_STATUS) {
                        return true;
                    }

                    return false;
                };

                // Apply the predicate to filter the collection
                return queryable.Where(predicate);
            }

            // If no Status property, return the original collection unfiltered
            return queryable;
        }
        #endregion

        #region Select 
        public static async Task<IEnumerable<TResult>> SelectListAsync<TEntity, TResult>(
            this IEnumerable<TEntity> source,
            Func<TEntity, Task<TResult>> selector) {

            var tasks = source.Select(selector); // Creates tasks for each item in the collection

            return await Task.WhenAll(tasks);    // Waits for all tasks to complete and returns the results

        }

        public static async Task<IEnumerable<TResult>> SelectListAsync<TEntity, TResult>(
            this IEnumerable<TEntity> source,
            Func<TEntity, Task<TResult>> selector,
            Func<TResult, bool> filter,
            int dataLimit = 100) {
            // Use Parallel Processing
            var tasks = source
                .Select(async item => (Result: await selector(item), IsValid: false))
                .ToList();

            // Await all the tasks to complete
            var results = await Task.WhenAll(tasks);

            // Filter and take only the limited number of items
            return results
                .Select(t => t.Result)
                .Where(filter)
                .Take(dataLimit);
        }

        public static IEnumerable<TEntity> DataLimit<TEntity>(
            this IEnumerable<TEntity> source,
            int dataLimit) {

            return source.Take(dataLimit);

        }

        #endregion

    }
}
