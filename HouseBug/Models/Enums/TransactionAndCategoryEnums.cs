namespace HouseBug.Models.Enums
{
    public enum TransactionType
    {
        Expense = 0,
        Income = 1
    }
    
    public enum CategoryType
    {
        General = 0,
        Food = 1,
        Transport = 2,
        Entertainment = 3,
        Bills = 4,
        Health = 5,
        Education = 6,
        Shopping = 7,
        Travel = 8,
        Other = 9
    }
    
    public enum ReportPeriod
    {
        Daily = 0,
        Weekly = 1,
        Monthly = 2,
        Yearly = 3,
        Custom = 4
    }
}