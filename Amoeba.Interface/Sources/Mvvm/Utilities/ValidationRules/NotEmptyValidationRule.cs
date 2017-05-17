using System.Globalization;
using System.Windows.Controls;

namespace Amoeba.Interface
{
    public class NotEmptyValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return string.IsNullOrWhiteSpace((string)value)
                ? new ValidationResult(false, LanguagesManager.Instance.ValidationRule_NotEmpty_ErrorMessage)
                : ValidationResult.ValidResult;
        }
    }
}
