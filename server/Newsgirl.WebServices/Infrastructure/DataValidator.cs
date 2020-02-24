using Newsgirl.Shared.Infrastructure;

namespace Newsgirl.WebServices.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Validates objects with DataAnnotations.
    /// If a property has a `ValidateObjectAttribute` then it gets recursively validated.
    /// </summary>
    public static class DataValidator
    {
        public static Result Validate<T>(T obj)
        {
            bool isValid = true;

            var errorMessages = new List<string>();

            void InnerValidate(object instance)
            {
                var validResults = new List<ValidationResult>();

                bool innerIsValid =
                    Validator.TryValidateObject(instance, new ValidationContext(instance), validResults, true);

                var innerErrorMessages = validResults.Select(v => v.ErrorMessage).ToArray();

                if (!innerIsValid)
                {
                    isValid = false;
                }

                errorMessages.AddRange(innerErrorMessages);

                foreach (var propertyInfo in instance.GetType().GetProperties())
                {
                    bool shouldValidate = propertyInfo.GetCustomAttribute<ValidateObjectAttribute>() != null;

                    if (shouldValidate)
                    {
                        InnerValidate(propertyInfo.GetValue(obj));
                    }
                }
            }

            InnerValidate(obj);

            if (!isValid)
            {
                return Result.FromErrorMessages(errorMessages.ToArray());
            }

            return Result.Success();
        }
    }

    /// <summary>
    /// Use on properties to have them validated. Works recursively.
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ValidateObjectAttribute : Attribute
    {
    }
}