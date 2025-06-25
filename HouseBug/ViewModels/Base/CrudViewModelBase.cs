using System;
using System.Threading.Tasks;

namespace HouseBug.ViewModels.Base
{
    public abstract class CrudViewModelBase<T> : ValidatableViewModelBase where T : class
    {
        protected bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        protected abstract Task<bool> SaveItemAsync(T item);

        protected abstract Task<bool> DeleteItemAsync(T item);

        protected abstract void ClearForm();

        protected abstract T CreateItemFromInput();

        protected abstract void PopulateFormFromItem(T item);

        protected virtual void StartAddItem()
        {
            ClearForm();
            IsEditMode = true;
            ValidationMessage = string.Empty;
        }

        protected virtual void CancelEdit()
        {
            IsEditMode = false;
            ClearForm();
            ValidationMessage = string.Empty;
        }
    }
}
