using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace Amoeba.Interface
{
    public class UriValidationRule : ValidationRule
    {
        private Regex _regex = new Regex("^(.+):(.+)$");

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (string.IsNullOrWhiteSpace(value as string) || !_regex.IsMatch((string)value))
            {
                return new ValidationResult(false, LanguagesManager.Instance.ValidationRule_ErrorMessage);
            }

            return ValidationResult.ValidResult;
        }
    }
}
