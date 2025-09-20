using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

class AccountRepository
{
    private readonly string _connStr;
    public AccountRepository(string connStr) { _connStr = connStr; }

    public void CreateAccount(Account account)
    {
        using var conn = new SqlConnection(_connStr);
        conn.Open();
        const string q = @"INSERT INTO Accounts (Accountnumber, Name, Password, Balance)
                           VALUES (@acc, @n, @p, @b)";
        using var cmd = new SqlCommand(q, conn);
        cmd.Parameters.AddWithValue("@acc", account.AccountNumber);
        cmd.Parameters.AddWithValue("@n", account.Name);
        cmd.Parameters.AddWithValue("@p", account.Password);
        cmd.Parameters.AddWithValue("@b", account.Balance);
        cmd.ExecuteNonQuery();
    }

    public Account? GetByCredentials(string accNo, string password)
    {
        using var conn = new SqlConnection(_connStr);
        conn.Open();
        const string q = @"SELECT Accountnumber, Name, Password, Balance
                           FROM Accounts WHERE Accountnumber=@acc AND Password=@p";
        using var cmd = new SqlCommand(q, conn);
        cmd.Parameters.AddWithValue("@acc", accNo);
        cmd.Parameters.AddWithValue("@p", password);
        using var r = cmd.ExecuteReader();
        return r.Read() ? new Account(r.GetString(0), r.GetString(2), r.GetString(1), r.GetDecimal(3)) : null;
    }

    public Account? GetByAccountNumber(string accNo)
    {
        using var conn = new SqlConnection(_connStr);
        conn.Open();
        const string q = @"SELECT Accountnumber, Name, Password, Balance
                           FROM Accounts WHERE Accountnumber=@acc";
        using var cmd = new SqlCommand(q, conn);
        cmd.Parameters.AddWithValue("@acc", accNo);
        using var r = cmd.ExecuteReader();
        return r.Read() ? new Account(r.GetString(0), r.GetString(2), r.GetString(1), r.GetDecimal(3)) : null;
    }

    public List<Account> GetAll()
    {
        var list = new List<Account>();
        using var conn = new SqlConnection(_connStr);
        conn.Open();
        const string q = @"SELECT Accountnumber, Name, Password, Balance FROM Accounts";
        using var cmd = new SqlCommand(q, conn);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new Account(r.GetString(0), r.GetString(2), r.GetString(1), r.GetDecimal(3)));
        return list;
    }

    public void UpdateBalance(string accNo, decimal newBalance)
    {
        using var conn = new SqlConnection(_connStr);
        conn.Open();
        const string q = @"UPDATE Accounts SET Balance=@b WHERE Accountnumber=@acc";
        using var cmd = new SqlCommand(q, conn);
        cmd.Parameters.AddWithValue("@b", newBalance);
        cmd.Parameters.AddWithValue("@acc", accNo);
        cmd.ExecuteNonQuery();
    }

    public void UpdateAccount(string accNo, string newName, string newPassword)
    {
        using var conn = new SqlConnection(_connStr);
        conn.Open();
        const string q = @"UPDATE Accounts SET Name=@n, Password=@p WHERE Accountnumber=@acc";
        using var cmd = new SqlCommand(q, conn);
        cmd.Parameters.AddWithValue("@n", newName);
        cmd.Parameters.AddWithValue("@p", newPassword);
        cmd.Parameters.AddWithValue("@acc", accNo);
        cmd.ExecuteNonQuery();
    }

    public bool DeleteAccount(string accNo)
    {
        using var conn = new SqlConnection(_connStr);
        conn.Open();
        const string q = @"DELETE FROM Accounts WHERE Accountnumber=@acc";
        using var cmd = new SqlCommand(q, conn);
        cmd.Parameters.AddWithValue("@acc", accNo);
        return cmd.ExecuteNonQuery() == 1;
    }

    public bool Transfer(string fromAcc, string toAcc, decimal amount)
    {
        if (amount <= 0) return false;
        using var conn = new SqlConnection(_connStr);
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            using (var debit = new SqlCommand(
                @"UPDATE Accounts SET Balance = Balance - @amt WHERE Accountnumber=@acc AND Balance >= @amt",
                conn, tx))
            {
                debit.Parameters.AddWithValue("@amt", amount);
                debit.Parameters.AddWithValue("@acc", fromAcc);
                if (debit.ExecuteNonQuery() != 1) { tx.Rollback(); return false; }
            }

            using (var credit = new SqlCommand(
                @"UPDATE Accounts SET Balance = Balance + @amt WHERE Accountnumber=@acc",
                conn, tx))
            {
                credit.Parameters.AddWithValue("@amt", amount);
                credit.Parameters.AddWithValue("@acc", toAcc);
                if (credit.ExecuteNonQuery() != 1) { tx.Rollback(); return false; }
            }

            tx.Commit();
            return true;
        }
        catch { tx.Rollback(); throw; }
    }
}

class Account
{
    public string AccountNumber { get; private set; }
    public string Password { get; private set; }
    public string Name { get; private set; }
    public decimal Balance { get; private set; }

    public Account(string accNo, string password, string name, decimal balance)
    {
        AccountNumber = accNo;
        Password = password;
        Name = name;
        Balance = balance;
    }

    public void Deposit(decimal amt) { if (amt > 0) Balance += amt; }
    public bool Withdraw(decimal amt) { if (amt > 0 && amt <= Balance) { Balance -= amt; return true; } return false; }
    public void ShowBalance() => Console.WriteLine($"Balance: {Balance}");
}

class ATM
{
    private readonly AccountRepository repo;
    private Account loggedIn;

    public ATM(string connStr) { repo = new AccountRepository(connStr); }

    public void Start()
    {
        while (true)
        {
            if (loggedIn == null)
            {
                Console.WriteLine("1- Create Account");
                Console.WriteLine("2- Login");
                Console.WriteLine("3- List Accounts");
                Console.WriteLine("4- Exit");
                Console.Write("Select: ");
                var opt = Console.ReadLine();
                if (opt == "1") CreateAccount();
                else if (opt == "2") Login();
                else if (opt == "3") ListAccounts();
                else if (opt == "4") return;
            }
            else
            {
                Console.WriteLine("1- Withdraw");
                Console.WriteLine("2- Deposit");
                Console.WriteLine("3- Transfer");
                Console.WriteLine("4- Check Balance");
                Console.WriteLine("5- Update Profile");
                Console.WriteLine("6- Delete Account");
                Console.WriteLine("7- Logout");
                Console.Write("Select: ");
                var opt = Console.ReadLine();
                if (opt == "1") DoWithdraw();
                else if (opt == "2") DoDeposit();
                else if (opt == "3") DoTransfer();
                else if (opt == "4") loggedIn.ShowBalance();
                else if (opt == "5") UpdateProfile();
                else if (opt == "6") DeleteMe();
                else if (opt == "7") loggedIn = null;
            }
        }
    }

    void CreateAccount()
    {
        Console.Write("Name: "); var n = Console.ReadLine();
        Console.Write("Account No: "); var acc = Console.ReadLine();
        Console.Write("Password: "); var p = Console.ReadLine();
        Console.Write("Deposit: "); var b = decimal.Parse(Console.ReadLine());
        var a = new Account(acc, p, n, b);
        repo.CreateAccount(a);
        Console.WriteLine("Created.");
    }

    void Login()
    {
        Console.Write("Account No: "); var acc = Console.ReadLine();
        Console.Write("Password: "); var p = Console.ReadLine();
        loggedIn = repo.GetByCredentials(acc, p);
        Console.WriteLine(loggedIn != null ? $"Welcome {loggedIn.Name}" : "Invalid login");
    }

    void ListAccounts()
    {
        foreach (var a in repo.GetAll())
            Console.WriteLine($"{a.AccountNumber} | {a.Name} | {a.Balance}");
    }

    void DoDeposit()
    {
        Console.Write("Amount: "); var amt = decimal.Parse(Console.ReadLine());
        loggedIn.Deposit(amt);
        repo.UpdateBalance(loggedIn.AccountNumber, loggedIn.Balance);
        Console.WriteLine("Deposited.");
    }

    void DoWithdraw()
    {
        Console.Write("Amount: "); var amt = decimal.Parse(Console.ReadLine());
        if (loggedIn.Withdraw(amt))
        {
            repo.UpdateBalance(loggedIn.AccountNumber, loggedIn.Balance);
            Console.WriteLine("Withdrawn.");
        }
        else Console.WriteLine("Invalid/insufficient.");
    }

    void DoTransfer()
    {
        Console.Write("Receiver: "); var recv = Console.ReadLine();
        Console.Write("Amount: "); var amt = decimal.Parse(Console.ReadLine());
        if (repo.Transfer(loggedIn.AccountNumber, recv, amt))
        {
            loggedIn = repo.GetByAccountNumber(loggedIn.AccountNumber);
            Console.WriteLine("Transferred.");
        }
        else Console.WriteLine("Transfer failed.");
    }

    void UpdateProfile()
    {
        Console.Write("New Name: "); var n = Console.ReadLine();
        Console.Write("New Password: "); var p = Console.ReadLine();
        repo.UpdateAccount(loggedIn.AccountNumber, n, p);
        Console.WriteLine("Updated.");
    }

    void DeleteMe()
    {
        if (repo.DeleteAccount(loggedIn.AccountNumber))
        {
            Console.WriteLine("Deleted. Logging out.");
            loggedIn = null;
        }
        else Console.WriteLine("Delete failed.");
    }
}

class Program
{
    private const string ConnStr =
        @"Server=localhost;Database=Atmdatabase;Trusted_Connection=True;TrustServerCertificate=True;";

    static void Main(string[] args)
    {
        var atm = new ATM(ConnStr);
        atm.Start();
    }
}
