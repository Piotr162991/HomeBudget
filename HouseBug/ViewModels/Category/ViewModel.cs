using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using HouseBug.Models;
using HouseBug.Services;
using HouseBug.ViewModels.Base;

namespace HouseBug.ViewModels
{
    public class CategoryViewModel : ViewModelBase, IDataErrorInfo
    {
        private readonly BudgetManager _budgetManager;

        public CategoryViewModel(BudgetManager budgetManager)
        {
            _budgetManager = budgetManager;
            InitializeCommands();
            LoadCategories();
        }

        #region Properties

        private ObservableCollection<Category> _categories;
        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        private Category _selectedCategory;
        public Category SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    PopulateEditForm();
                }
            }
        }

        private string _name;
        [Required(ErrorMessage = "Nazwa jest wymagana")]
        [StringLength(50, ErrorMessage = "Nazwa nie może być dłuższa niż 50 znaków")]
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _description;
        [StringLength(200, ErrorMessage = "Opis nie może być dłuższy niż 200 znaków")]
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private string _color = "#3498DB";
        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        private string _icon;
        public string Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        private bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        private string _validationMessage;
        public string ValidationMessage
        {
            get => _validationMessage;
            set => SetProperty(ref _validationMessage, value);
        }

        // Predefiniowane kolory
        public string[] PredefinedColors { get; } = 
        {
            "#E74C3C", "#3498DB", "#2ECC71", "#F39C12", "#9B59B6",
            "#1ABC9C", "#E67E22", "#34495E", "#F1C40F", "#E91E63"
        };

        // Predefiniowane ikony
        public string[] PredefinedIcons { get; } = 
        {
            "🍕", "🚗", "🎮", "💡", "💰", "⚕️", "🛍️", "📚", "🏠", "✈️"
        };

        #endregion

        #region Commands

        public ICommand AddCategoryCommand { get; private set; }
        public ICommand SaveCategoryCommand { get; private set; }
        public ICommand DeleteCategoryCommand { get; private set; }
        public ICommand CancelEditCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }

        private void InitializeCommands()
        {
            AddCategoryCommand = new RelayCommand(StartAddCategory);
            SaveCategoryCommand = new RelayCommand(SaveCategory, CanSaveCategory);
            DeleteCategoryCommand = new RelayCommand(DeleteCategory, () => SelectedCategory != null && !IsEditMode);
            CancelEditCommand = new RelayCommand(CancelEdit);
            RefreshCommand = new RelayCommand(RefreshCategories);
        }

        #endregion

        #region Command Methods

        private void StartAddCategory()
        {
            ClearForm();
            IsEditMode = true;
            ValidationMessage = string.Empty;
        }

        private async void SaveCategory()
        {
            if (!IsValid()) return;

            SetBusy(true, "Zapisywanie kategorii...");

            try
            {
                var category = CreateCategoryFromInput();

                if (SelectedCategory?.Id > 0)
                {
                    // Edycja istniejącej kategorii
                    category.Id = SelectedCategory.Id;
                    var success = await _budgetManager.UpdateCategoryAsync(category);
                    
                    if (success)
                    {
                        var index = Categories.IndexOf(SelectedCategory);
                        Categories[index] = category;
                        SelectedCategory = category;
                        ValidationMessage = "Kategoria została zaktualizowana.";
                    }
                    else
                    {
                        ValidationMessage = "Błąd podczas aktualizacji kategorii.";
                    }
                }
                else
                {
                    // Dodawanie nowej kategorii
                    var savedCategory = await _budgetManager.AddCategoryAsync(category);
                    Categories.Add(savedCategory);
                    SelectedCategory = savedCategory;
                    ValidationMessage = "Kategoria została dodana.";
                }

                IsEditMode = false;
            }
            catch (Exception ex)
            {
                ValidationMessage = $"Wystąpił błąd: {ex.Message}";
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void DeleteCategory()
        {
            if (SelectedCategory == null) return;

            // Tutaj powinna być logika potwierdzenia usunięcia
            var confirmResult = ShowConfirmationDialog(
                $"Czy na pewno chcesz usunąć kategorię '{SelectedCategory.Name}'?\n" +
                "Jeśli kategoria ma przypisane transakcje, zostanie tylko dezaktywowana.");

            if (!confirmResult) return;

            SetBusy(true, "Usuwanie kategorii...");

            try
            {
                var success = await _budgetManager.DeleteCategoryAsync(SelectedCategory.Id);
                
                if (success)
                {
                    Categories.Remove(SelectedCategory);
                    SelectedCategory = null;
                    ClearForm();
                    ValidationMessage = "Kategoria została usunięta.";
                }
                else
                {
                    ValidationMessage = "Błąd podczas usuwania kategorii.";
                }
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void CancelEdit()
        {
            IsEditMode = false;
            ClearForm();
            ValidationMessage = string.Empty;
        }

        private async void RefreshCategories()
        {
            SetBusy(true, "Odświeżanie kategorii...");
            
            try
            {
                await LoadCategoriesAsync();
                ValidationMessage = "Lista kategorii została odświeżona.";
            }
            finally
            {
                SetBusy(false);
            }
        }

        private bool CanSaveCategory()
        {
            return !IsBusy && IsValid() && IsEditMode;
        }

        #endregion

        #region Data Loading

        private async void LoadCategories()
        {
            await LoadCategoriesAsync();
        }

        private async Task LoadCategoriesAsync()
        {
            SetBusy(true, "Ładowanie kategorii...");
            
            try
            {
                var categories = await Task.Run(() => _budgetManager.GetAllCategories());
                Categories = new ObservableCollection<Category>(categories);
            }
            finally
            {
                SetBusy(false);
            }
        }

        #endregion

        #region Validation

        public string Error => string.Empty;

        public string this[string columnName]
        {
            get
            {
                string error = string.Empty;

                switch (columnName)
                {
                    case nameof(Name):
                        if (string.IsNullOrWhiteSpace(Name))
                            error = "Nazwa jest wymagana";
                        else if (Name.Length > 50)
                            error = "Nazwa nie może być dłuższa niż 50 znaków";
                        else if (Categories?.Any(c => c.Name.Equals(Name, StringComparison.OrdinalIgnoreCase) && 
                                                     c.Id != (SelectedCategory?.Id ?? 0)) == true)
                            error = "Kategoria o tej nazwie już istnieje";
                        break;

                    case nameof(Description):
                        if (!string.IsNullOrEmpty(Description) && Description.Length > 200)
                            error = "Opis nie może być dłuższy niż 200 znaków";
                        break;
                }

                return error;
            }
        }

        private bool IsValid()
        {
            var properties = new[] { nameof(Name), nameof(Description) };
            var hasErrors = properties.Any(property => !string.IsNullOrEmpty(this[property]));
            
            if (hasErrors)
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(Name);
        }

        #endregion

        #region Helper Methods

        private void PopulateEditForm()
        {
            if (SelectedCategory != null)
            {
                Name = SelectedCategory.Name;
                Description = SelectedCategory.Description;
                Color = SelectedCategory.Color;
                Icon = SelectedCategory.Icon;
                IsActive = SelectedCategory.IsActive;
            }
        }

        private void ClearForm()
        {
            Name = string.Empty;
            Description = string.Empty;
            Color = "#3498DB";
            Icon = string.Empty;
            IsActive = true;
        }

        private Category CreateCategoryFromInput()
        {
            return new Category
            {
                Name = Name?.Trim(),
                Description = Description?.Trim(),
                Color = Color,
                Icon = Icon,
                IsActive = IsActive
            };
        }

        private bool ShowConfirmationDialog(string message)
        {
            // Implementacja zależna od warstwy widoku
            throw new NotImplementedException("Metoda powinna być zaimplementowana w warstwie widoku");
        }

        #endregion
    }
}