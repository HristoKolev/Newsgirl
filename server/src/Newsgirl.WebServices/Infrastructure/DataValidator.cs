namespace Newsgirl.WebServices.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Reflection;

    public static class DataValidator
    {
        public static (bool, string[]) Validate<T>(T obj)
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

            return (isValid, errorMessages.ToArray());
        }
    }

    public class ValidateObjectAttribute : Attribute
    {
    }
}