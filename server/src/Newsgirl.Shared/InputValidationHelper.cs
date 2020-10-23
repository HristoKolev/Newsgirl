namespace Newsgirl.Shared
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;

    public class InputValidator
    {
        public static RpcResult Validate<T>(T obj)
        {
            bool isValid = true;

            var errorMessages = new List<string>();

            void InnerValidate(object instance)
            {
                var validResults = new List<ValidationResult>();

                bool innerIsValid = Validator.TryValidateObject(instance, new ValidationContext(instance), validResults, true);

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

            if (isValid)
            {
                return RpcResult.Ok();
            }

            return RpcResult.Error(errorMessages.ToArray());
        }
    }

    public class ValidateObjectAttribute : Attribute { }

    public class EmailAttribute : ValidationAttribute
    {
        public EmailAttribute()
        {
            ErrorMessage = "Please, enter a valid email address.";
        }

        private readonly Regex emailRegex =
            new Regex(@"(([^<>()\[\].,;:\s@""]+(\.[^<>()\[\].,;:\s@""]+)*)|("".+""))@(([^<>()[\].,;:\s@""]+\.)+[^<>()[\].,;:\s@""]{2,})",
                RegexOptions.Compiled);

        public override bool IsValid(object value)
        {
            return string.IsNullOrWhiteSpace((string) value) || this.emailRegex.IsOnlyMatch((string) value);
        }
    }
}
