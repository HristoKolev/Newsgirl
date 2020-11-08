namespace Newsgirl.Shared
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;

    public static class InputValidator
    {
        public static RpcResult Validate<T>(T obj)
        {
            bool isValid = true;

            var errorMessages = new List<string>();

            void InnerValidate(object instance)
            {
                var validResults = new List<ValidationResult>();

                bool innerIsValid = Validator.TryValidateObject(
                    instance,
                    new ValidationContext(instance),
                    validResults,
                    true
                );

                if (!innerIsValid)
                {
                    isValid = false;
                }

                errorMessages.AddRange(validResults.Select(v => v.ErrorMessage));

                foreach (var propertyInfo in instance.GetType().GetProperties())
                {
                    bool shouldValidate = propertyInfo.GetCustomAttribute<ValidatePropertyAttribute>() != null;

                    if (shouldValidate)
                    {
                        InnerValidate(propertyInfo.GetValue(obj));
                    }
                }
            }

            InnerValidate(obj);

            if (isValid)
            {
                return RpcResult.Ok();
            }

            return RpcResult.Error(errorMessages.ToArray());
        }
    }

    /// <summary>
    /// Apply this attribute to properties of CLR types that should also be validated.
    /// </summary>
    public class ValidatePropertyAttribute : Attribute { }

    public class EmailAttribute : ValidationAttribute
    {
        public EmailAttribute()
        {
            this.ErrorMessage = "Please, enter a valid email address.";
        }

        private static readonly Regex EmailRegex =
            new Regex(@"(([^<>()\[\].,;:\s@""]+(\.[^<>()\[\].,;:\s@""]+)*)|("".+""))@(([^<>()[\].,;:\s@""]+\.)+[^<>()[\].,;:\s@""]{2,})",
                RegexOptions.Compiled);

        public override bool IsValid(object value)
        {
            return string.IsNullOrWhiteSpace((string) value) || EmailRegex.IsOnlyMatch((string) value);
        }
    }
}
