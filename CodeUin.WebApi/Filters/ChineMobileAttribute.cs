using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace CodeUin.WebApi.Filters
{
    public class ChineMobileAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (!(value is string)) return false;

            var val = (string)value;

            return Regex.IsMatch(val, @"^[1]{1}[2,3,4,5,6,7,8,9]{1}\d{9}$");
        }
    }
}