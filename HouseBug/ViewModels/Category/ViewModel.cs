using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using HouseBug.Models;
using HouseBug.Services;
using HouseBug.ViewModels.Base;

namespace HouseBug.ViewModels
{
    public class CategoryViewModel : CrudViewModelBase<Category>
    {
        private readonly BudgetManager _budgetManager;
        private readonly IDialogService _dialogService;

        public CategoryViewModel() : this(new BudgetManager(), new DialogService())
        {
        }

        public CategoryViewModel(BudgetManager budgetManager, IDialogService dialogService)
        {
            _budgetManager = budgetManager;
            _dialogService = dialogService;
            InitializeCommands();
            LoadCategories();
        }

        #region Properties

        private ObservableCollection<Category> _categories;
        public ObservableCollection<Category> Categories
        {
            get => _categories ??= new ObservableCollection<Category>();
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
                    PopulateFormFromItem(value);
                }
            }
        }

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _description;
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
        public ICommand SelectColorCommand { get; private set; }
        public ICommand SelectIconCommand { get; private set; }

        private void InitializeCommands()
        {
            AddCategoryCommand = CommandFactory.Create(StartAddCategory);
            SaveCategoryCommand = CommandFactory.Create(SaveCategory, () => !IsBusy && IsValid() && IsEditMode);
            DeleteCategoryCommand = CommandFactory.Create(DeleteCategory, () => SelectedCategory != null && !IsEditMode);
            CancelEditCommand = CommandFactory.Create(CancelEdit);
            RefreshCommand = CommandFactory.Create(RefreshCategories);
            SelectColorCommand = CommandFactory.Create<string>(SelectColor);
            SelectIconCommand = CommandFactory.Create<string>(SelectIcon);
        }

        #endregion

        #region Command Methods

        private void StartAddCategory() => StartAddItem();

        private async void SaveCategory()
        {
            if (!IsValid()) return;

            await HandleOperationAsync("zapisywanie kategorii", async () =>
            {
                var category = CreateItemFromInput();

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
            });
        }

        private void SelectColor(string color)
        {
            if (!string.IsNullOrEmpty(color))
            {
                Color = color;
            }
        }

        private void SelectIcon(string icon)
        {
            if (!string.IsNullOrEmpty(icon))
            {
                Icon = icon;
            }
        }

        private async void DeleteCategory()
        {
            if (SelectedCategory == null) return;

            var confirmResult = _dialogService.ShowConfirmation(
                $"Czy na pewno chcesz usunąć kategorię '{SelectedCategory.Name}'?\n" +
                "Jeśli kategoria ma przypisane transakcje, zostanie tylko dezaktywowana.");

            if (!confirmResult) return;

            await HandleOperationAsync("usuwanie kategorii", async () =>
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
            });
        }

        private async void RefreshCategories()
        {
            await HandleOperationAsync("odświeżanie kategorii", async () =>
            {
                await LoadCategoriesAsync();
                ValidationMessage = "Lista kategorii została odświeżona.";
            });
        }

        #endregion

        #region Data Loading

        private async void LoadCategories()
        {
            await LoadCategoriesAsync();
        }

        private async Task LoadCategoriesAsync()
        {
            await HandleOperationAsync("ładowanie kategorii", async () =>
            {
                var categories = await Task.Run(() => _budgetManager.GetAllCategories());
                Categories = new ObservableCollection<Category>(categories);
            });
        }

        #endregion

        #region Validation

        protected override string[] GetValidatableProperties()
        {
            return new[] { nameof(Name), nameof(Description) };
        }

        protected override string GetValidationError(string propertyName)
        {
            string error = string.Empty;

            switch (propertyName)
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

        protected override bool IsValid()
        {
            var isValid = base.IsValid();
            if (!isValid)
            {
                ValidationMessage = "Proszę poprawić błędy walidacji.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                ValidationMessage = "Nazwa jest wymagana.";
                return false;
            }

            ValidationMessage = string.Empty;
            return true;
        }

        #endregion

        #region Override Methods

        protected override void PopulateFormFromItem(Category item)
        {
            if (item != null)
            {
                Name = item.Name;
                Description = item.Description;
                Color = item.Color;
                Icon = item.Icon;
                IsActive = item.IsActive;
            }
        }

        protected override void ClearForm()
        {
            Name = string.Empty;
            Description = string.Empty;
            Color = "#3498DB";
            Icon = string.Empty;
            IsActive = true;
        }

        protected override Category CreateItemFromInput()
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

        protected override async Task<bool> SaveItemAsync(Category item)
        {
            if (item.Id > 0)
            {
                return await _budgetManager.UpdateCategoryAsync(item);
            }
            else
            {
                var savedItem = await _budgetManager.AddCategoryAsync(item);
                return savedItem != null;
            }
        }

        protected override async Task<bool> DeleteItemAsync(Category item)
        {
            return await _budgetManager.DeleteCategoryAsync(item.Id);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _budgetManager?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}