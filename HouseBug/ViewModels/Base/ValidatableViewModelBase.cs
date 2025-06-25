using System.ComponentModel;
using System.Linq;

namespace HouseBug.ViewModels.Base
{
    public abstract class ValidatableViewModelBase : ViewModelBase, IDataErrorInfo
    {
        public string Error => string.Empty;

        public string this[string columnName]
        {
            get
            {
                return GetValidationError(columnName);
            }
        }

        protected virtual bool IsValid()
        {
            return !GetValidatableProperties().Any(property => !string.IsNullOrEmpty(GetValidationError(property)));
        }

        protected abstract string GetValidationError(string propertyName);

        protected abstract string[] GetValidatableProperties();
    }
}
