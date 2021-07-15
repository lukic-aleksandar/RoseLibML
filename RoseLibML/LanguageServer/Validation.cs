using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace RoseLibML.LanguageServer
{
    class Validation
    {
        public static List<string> ValidateArguments(object arguments)
        {
            List<string> validationMessages = new List<string>();

            ValidationContext context = new ValidationContext(arguments, null, null);
            List<ValidationResult> validationResults = new List<ValidationResult>();
            bool valid = Validator.TryValidateObject(arguments, context, validationResults, true);
            if (!valid)
            {
                foreach (ValidationResult validationResult in validationResults)
                {
                    validationMessages.Add(validationResult.ErrorMessage);
                }
            }

            return validationMessages;
        }
    }

    public class DirectoryExists : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            string path = value as string;
            if (Directory.Exists(path))
            {
                return true;
            }

            return false;
        }
    }

    public class FileDirectoryExists : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            string path = value as string;
            if (Directory.Exists(Path.GetDirectoryName(path)))
            {
                return true;
            }

            return false;
        }
    }

    public class FileExists : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            string path = value as string;
            if (File.Exists(path))
            {
                return true;
            }

            return false;
        }
    }
}
