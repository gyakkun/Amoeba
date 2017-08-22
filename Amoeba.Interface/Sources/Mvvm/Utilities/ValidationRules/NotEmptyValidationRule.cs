using System.Globalization;
using System.Windows.Controls;

namespace Amoeba.Interface
{
    public class NotEmptyValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (string.IsNullOrWhiteSpace(value as string))
            {
                return new ValidationResult(false, LanguagesManager.Instance.ValidationRule_ErrorMessage);
            }

            return ValidationResult.ValidResult;
        }
    }
}
