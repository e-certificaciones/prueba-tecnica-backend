using Banking.Domain.Enums;

namespace Banking.Domain.Entities;

public sealed class Customer
{
    public Guid Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public DateOnly DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public decimal MonthlyIncome { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Customer()
    {
    }

    public Customer(string fullName, DateOnly dateOfBirth, Gender gender, decimal monthlyIncome, DateTime createdAt)
    {
        Id = Guid.NewGuid();
        FullName = fullName;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        MonthlyIncome = monthlyIncome;
        CreatedAt = createdAt;
    }

    public void Update(string fullName, DateOnly dateOfBirth, Gender gender, decimal monthlyIncome)
    {
        FullName = fullName;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        MonthlyIncome = monthlyIncome;
    }
}
