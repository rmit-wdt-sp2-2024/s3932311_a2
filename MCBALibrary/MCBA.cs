using SimpleHashing.Net;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Diagnostics;

namespace MCBALibrary
{
    public class MCBA
    {

        //Use Sql to find id of next transaction
        public static int GetTransactionCount(string connectionStr)
        {
            //Get transactionID
            int count = 0;
            using (SqlConnection connection = new SqlConnection(connectionStr))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) from [Transaction]";
                count = (Int32)command.ExecuteScalar();
            }
            return count;
        }

        //Get list of all transactions of account
        public static List<Transaction> GetTransactionList(string connectionStr, string accountNumber)
        {
            List<Transaction> transactionList = new List<Transaction>();

            using (SqlConnection connection = new SqlConnection(connectionStr))
            {
                connection.Open();

                //Customer
                var command = connection.CreateCommand();
                command.CommandText = "Select * from [Transaction] WHERE AccountNumber = @accountNumber ORDER BY TransactionTimeUtc DESC";
                command.Parameters.AddWithValue("AccountNumber", accountNumber);
                SqlDataReader reader = command.ExecuteReader();
                var transactionID = 0;

                while (reader.Read())
                {
                    int? destAccNum = null;
                    string? comm = null;
                    if (reader["DestinationAccountNumber"] != DBNull.Value)
                    {
                        destAccNum = Convert.ToInt32(reader["DestinationAccountNumber"]);
                    }
                    if (reader["Comment"] != DBNull.Value)
                    {
                        comm = reader["Comment"].ToString();
                    }
                    Transaction transaction = new Transaction
                    {
                        transactionID = Convert.ToInt32(reader["TransactionID"]),
                        transactionType = reader["TransactionType"].ToString(),
                        accountNumber = Convert.ToInt32(reader["AccountNumber"]),
                        destinationAccountNumber = destAccNum,
                        amount = float.Parse(reader["Amount"].ToString()),
                        comment = comm,
                        transactionTimeUtc = reader["TransactionTimeUtc"].ToString()
                    };
                    DateTime d = DateTime.Parse(transaction.transactionTimeUtc).ToLocalTime();
                    transaction.transactionTimeUtc = transaction.transactionTimeUtc.ToString();
                    transactionList.Add(transaction);
                }
                reader.Close();
            }
            return transactionList;
        }

        // Creates a new transaction object and inserts it into the database
        public static List<Customer> TransferFrom(List<Customer> customerList, Customer customer, string connectionStr, int accIndex, string destAccNum, int traID, float amnt, string comm)
        {
            //New Transaction Object with User Input Values
            Transaction transaction = new Transaction
            {
                transactionID = traID,
                transactionType = "T",
                accountNumber = customer.accounts[accIndex].accountNumber,
                destinationAccountNumber = Int32.Parse(destAccNum),
                amount = amnt,
                comment = comm,
                transactionTimeUtc = DateTime.UtcNow.ToString(),
            };
            customerList[customerList.IndexOf(customer)].accounts[accIndex].transactions.Add(transaction);
            customerList[customerList.IndexOf(customer)].accounts[accIndex].balance -= transaction.amount;

            //Add New Transaction to SQL
            using (SqlConnection connection = new SqlConnection(connectionStr))
            {
                connection.Open();
                transaction.InsertRow(connection);

                //Update Account Balance
                var command = connection.CreateCommand();
                command.CommandText =
                    "UPDATE Account SET Balance = @balance WHERE AccountNumber = @accountNumber;";

                command.Parameters.AddWithValue("balance", customerList[customerList.IndexOf(customer)].accounts[accIndex].balance);
                command.Parameters.AddWithValue("accountNumber", customerList[customerList.IndexOf(customer)].accounts[accIndex].accountNumber);
                command.ExecuteNonQuery();
            }
            return customerList;
        }

        // Creates a new transaction object for receiving party and inserts it into the database
        public static List<Customer> TransferTo(List<Customer> customerList, Customer customer, string connectionStr, int accIndex, int traID, float amnt, string comm)
        {
            //New Transaction Object with User Input Values
            Transaction transaction = new Transaction
            {
                transactionID = traID,
                transactionType = "T",
                accountNumber = customer.accounts[accIndex].accountNumber,
                destinationAccountNumber = null,
                amount = amnt,
                comment = comm,
                transactionTimeUtc = DateTime.UtcNow.ToString(),
            };
            customerList[customerList.IndexOf(customer)].accounts[accIndex].transactions.Add(transaction);
            customerList[customerList.IndexOf(customer)].accounts[accIndex].balance += transaction.amount;

            //Add New Transaction to SQL
            using (SqlConnection connection = new SqlConnection(connectionStr))
            {
                connection.Open();
                transaction.InsertRow(connection);
                //Update Account Balance
                var command = connection.CreateCommand();
                command.CommandText =
                    "UPDATE Account SET Balance = @balance WHERE AccountNumber = @accountNumber;";

                command.Parameters.AddWithValue("balance", customerList[customerList.IndexOf(customer)].accounts[accIndex].balance);
                command.Parameters.AddWithValue("accountNumber", customerList[customerList.IndexOf(customer)].accounts[accIndex].accountNumber);
                command.ExecuteNonQuery();
            }
            return customerList;
        }

        // Creates a new transaction object for fee for sending party and inserts it into the database
        public static List<Customer> TransferFee(List<Customer> customerList, Customer customer, string connectionStr, int accIndex, int traID)
        {
            //New Transaction Object with User Input Values
            Transaction transaction = new Transaction
            {
                transactionID = traID,
                transactionType = "S",
                accountNumber = customer.accounts[accIndex].accountNumber,
                destinationAccountNumber = null,
                amount = (float)0.1,
                comment = "Transfer Fee",
                transactionTimeUtc = DateTime.UtcNow.ToString(),
            };
            customerList[customerList.IndexOf(customer)].accounts[accIndex].transactions.Add(transaction);
            customerList[customerList.IndexOf(customer)].accounts[accIndex].balance -= transaction.amount;

            //Add New Transaction to SQL
            using (SqlConnection connection = new SqlConnection(connectionStr))
            {
                connection.Open();
                transaction.InsertRow(connection);
                //Update Account Balance
                var command = connection.CreateCommand();
                command.CommandText =
                    "UPDATE Account SET Balance = @balance WHERE AccountNumber = @accountNumber;";

                command.Parameters.AddWithValue("balance", customerList[customerList.IndexOf(customer)].accounts[accIndex].balance);
                command.Parameters.AddWithValue("accountNumber", customerList[customerList.IndexOf(customer)].accounts[accIndex].accountNumber);
                command.ExecuteNonQuery();
            }
            return customerList;
        }

        // Creates a new transaction object for fee for sending party and inserts it into the database
        public static List<Customer> WithdrawFee(List<Customer> customerList, Customer customer, string connectionStr, int accIndex, int traID)
        {
            //New Deposit Transaction Object with User Input Values
            Transaction transaction = new Transaction
            {
                transactionID = traID,
                transactionType = "S",
                accountNumber = customer.accounts[accIndex].accountNumber,
                destinationAccountNumber = customer.accounts[accIndex].accountNumber,
                amount = (float)0.05,
                comment = "Withdrawal Fee",
                transactionTimeUtc = DateTime.UtcNow.ToString(),
            };
            customerList[customerList.IndexOf(customer)].accounts[accIndex].transactions.Add(transaction);
            customerList[customerList.IndexOf(customer)].accounts[accIndex].balance -= transaction.amount;

            //Add New Transaction to SQL
            using (SqlConnection connection = new SqlConnection(connectionStr))
            {
                connection.Open();
                transaction.InsertRow(connection);

                //Update Account Balance
                var command = connection.CreateCommand();
                command.CommandText =
                    "UPDATE Account SET Balance = @balance WHERE AccountNumber = @accountNumber;";

                command.Parameters.AddWithValue("balance", customerList[customerList.IndexOf(customer)].accounts[accIndex].balance);
                command.Parameters.AddWithValue("accountNumber", customerList[customerList.IndexOf(customer)].accounts[accIndex].accountNumber);
                command.ExecuteNonQuery();
            }
            return customerList;
        }

        // Creates a new transaction object for deposit for and inserts it into the database
        public static List<Customer> Deposit(List<Customer> customerList, Customer customer, string connectionStr, int accIndex, int traID, float amnt, string comm)
        {
            //New Deposit Transaction Object with User Input Values
            Transaction transaction = new Transaction
            {
                transactionID = traID,
                transactionType = "D",
                accountNumber = customer.accounts[accIndex].accountNumber,
                destinationAccountNumber = customer.accounts[accIndex].accountNumber,
                amount = amnt,
                comment = comm,
                transactionTimeUtc = DateTime.UtcNow.ToString(),
            };
            customerList[customerList.IndexOf(customer)].accounts[accIndex].transactions.Add(transaction);
            customerList[customerList.IndexOf(customer)].accounts[accIndex].balance += transaction.amount;

            //Add New Transaction to SQL
            using (SqlConnection connection = new SqlConnection(connectionStr))
            {
                connection.Open();
                transaction.InsertRow(connection);

                //Update Account Balance
                var command = connection.CreateCommand();
                command.CommandText =
                    "UPDATE Account SET Balance = @balance WHERE AccountNumber = @accountNumber;";

                command.Parameters.AddWithValue("balance", customerList[customerList.IndexOf(customer)].accounts[accIndex].balance);
                command.Parameters.AddWithValue("accountNumber", customerList[customerList.IndexOf(customer)].accounts[accIndex].accountNumber);
                command.ExecuteNonQuery();
            }
            return customerList;
        }

        // Creates a new transaction object for withdrawal and inserts it into the database
        public static List<Customer> Withdraw(List<Customer> customerList, Customer customer, string connectionStr, int accIndex, int traID, float amnt, string comm)
        {
            //New Deposit Transaction Object with User Input Values
            Transaction transaction = new Transaction
            {
                transactionID = traID,
                transactionType = "W",
                accountNumber = customer.accounts[accIndex].accountNumber,
                destinationAccountNumber = customer.accounts[accIndex].accountNumber,
                amount = amnt,
                comment = comm,
                transactionTimeUtc = DateTime.UtcNow.ToString(),
            };
            customerList[customerList.IndexOf(customer)].accounts[accIndex].transactions.Add(transaction);
            customerList[customerList.IndexOf(customer)].accounts[accIndex].balance -= transaction.amount;

            //Add New Transaction to SQL
            using (SqlConnection connection = new SqlConnection(connectionStr))
            {
                connection.Open();
                transaction.InsertRow(connection);

                //Update Account Balance
                var command = connection.CreateCommand();
                command.CommandText =
                    "UPDATE Account SET Balance = @balance WHERE AccountNumber = @accountNumber;";

                command.Parameters.AddWithValue("balance", customerList[customerList.IndexOf(customer)].accounts[accIndex].balance);
                command.Parameters.AddWithValue("accountNumber", customerList[customerList.IndexOf(customer)].accounts[accIndex].accountNumber);
                command.ExecuteNonQuery();
            }
            return customerList;
        }

        //Parse through customerList, replace null values
        public static List<Customer> EvaluateCustomerList(List<Customer> customerList, string connectionStr)
        {
            foreach (var customer in customerList)
            {
                customer.login.customerID = customer.customerID;
                if (customer.address == null)
                {
                    customer.address = "null";
                }
                if (customer.city == null)
                {
                    customer.city = "null";
                }
                if (customer.postCode == null)
                {
                    customer.postCode = 0;
                }
                foreach (var account in customer.accounts)
                {
                    float balance = 0;
                    foreach (var transaction in account.transactions)
                    {
                        balance += transaction.amount;
                        if (transaction.transactionType == null)
                        {
                            //Get transactionID
                            int count = 0;
                            using (SqlConnection connection = new SqlConnection(connectionStr))
                            {
                                connection.Open();
                                var command = connection.CreateCommand();
                                command.CommandText = "SELECT COUNT(*) from [Transaction]";
                                count = (Int32)command.ExecuteScalar();
                            }
                            transaction.transactionID = count;
                            transaction.transactionType = "D";
                            transaction.accountNumber = account.accountNumber;
                            transaction.destinationAccountNumber = account.accountNumber;
                        }
                    }
                    account.balance = balance;
                }
            }
            return customerList;
        }

        //Gets rows of objects of every class from database, creates new customerList
        public static List<Customer> LoadSQLData(string connectionStr)
        {
            List<Customer> customerList = new List<Customer>();
            using (SqlConnection connection = new SqlConnection(connectionStr))
            {
                connection.Open();

                //Customer
                var cusCommand = connection.CreateCommand();
                cusCommand.CommandText = "Select * from [Customer] WHERE CustomerID IS NOT NULL";
                SqlDataReader cusReader = cusCommand.ExecuteReader();

                while (cusReader.Read())
                {
                    int? pCode = null;
                    string? addr = null, cit = null;
                    if (cusReader["Address"] != DBNull.Value)
                    {
                        addr = cusReader["Address"].ToString();
                    }
                    if (cusReader["City"] != DBNull.Value)
                    {
                        cit = cusReader["City"].ToString();
                    }
                    if (cusReader["PostCode"] != DBNull.Value)
                    {
                        pCode = Convert.ToInt32(cusReader["PostCode"]);
                    }

                    var loginService = new LoginService();
                    var login = new Login { loginID = "0", passwordHash = "", customerID = 0 };
                    Customer customer = new Customer(0, "name", login, loginService)
                    {
                        login = login,
                        customerID = Convert.ToInt32(cusReader["CustomerID"]),
                        name = cusReader["Name"].ToString(),
                        address = addr,
                        city = cit,
                        postCode = pCode,
                        accounts = new List<Account>(),
                    };
                    customerList.Add(customer);
                }
                cusReader.Close();

                //Login
                var logCommand = connection.CreateCommand();
                logCommand.CommandText = "Select * from [Login] WHERE LoginID IS NOT NULL";
                SqlDataReader logReader = logCommand.ExecuteReader();
                while (logReader.Read())
                {
                    int cusIndex = customerList.FindIndex(customer => customer.customerID == Convert.ToInt32(logReader["CustomerID"]));

                    //Use Dependancy injection to update login
                    customerList[cusIndex].UpdateLogin(logReader["LoginID"].ToString(), logReader["PasswordHash"].ToString(), Convert.ToInt32(logReader["CustomerID"]));
                }
                logReader.Close();

                //Account
                var accCommand = connection.CreateCommand();
                accCommand.CommandText = "Select * from [Account] WHERE AccountNumber IS NOT NULL";
                SqlDataReader accReader = accCommand.ExecuteReader();

                while (accReader.Read())
                {
                    Account account = new Account
                    {
                        customerID = Convert.ToInt32(accReader["CustomerID"]),
                        accountType = accReader["AccountType"].ToString(),
                        accountNumber = Convert.ToInt32(accReader["AccountNumber"]),
                        balance = float.Parse(accReader["Balance"].ToString()),
                        transactions = new List<Transaction>()
                    };
                    customerList[customerList.FindIndex(customer => customer.customerID == Convert.ToInt32(accReader["CustomerID"]))].accounts.Add(account);
                }
                accReader.Close();

                //Transaction
                var traCommand = connection.CreateCommand();
                traCommand.CommandText = "Select * from [Transaction] WHERE TransactionID IS NOT NULL";
                SqlDataReader traReader = traCommand.ExecuteReader();

                while (traReader.Read())
                {
                    int cusIndex = -1;
                    int accIndex = -1;
                    foreach (var customer in customerList)
                    {
                        if (accIndex == -1)
                        {
                            cusIndex = customerList.IndexOf(customer);
                            accIndex = customer.accounts.FindIndex(account => account.accountNumber == Convert.ToInt32(traReader["AccountNumber"]));
                        }
                    }
                    int? destAccNum = null;
                    string? comm = null;
                    if (traReader["DestinationAccountNumber"] != DBNull.Value)
                    {
                        destAccNum = Convert.ToInt32(traReader["DestinationAccountNumber"]);
                    }
                    if (traReader["Comment"] != DBNull.Value)
                    {
                        comm = traReader["Comment"].ToString();
                    }

                    Transaction transaction = new Transaction
                    {
                        transactionID = Convert.ToInt32(traReader["TransactionID"]),
                        transactionType = traReader["TransactionType"].ToString(),
                        accountNumber = Convert.ToInt32(traReader["AccountNumber"]),
                        destinationAccountNumber = destAccNum,
                        amount = float.Parse(traReader["Amount"].ToString()),
                        comment = comm,
                        transactionTimeUtc = traReader["TransactionTimeUtc"].ToString(),
                    };
                    customerList[cusIndex].accounts[accIndex].transactions.Add(transaction);
                }
                traReader.Close();
            }
            return customerList;
        }

        //Inserts rows of objects of every class into database
        public static void InsertSQLData(string connectionStr, List<Customer> customerList)
        {
            using (SqlConnection connection = new SqlConnection(connectionStr))
            {
                connection.Open();
                foreach (var customer in customerList)
                {
                    customer.InsertRow(connection);
                    customer.login.InsertRow(connection);

                    foreach (var account in customer.accounts)
                    {
                        account.InsertRow(connection);

                        foreach (var transaction in account.transactions)
                        {
                            transaction.InsertRow(connection);
                        }
                    }
                }
            }
        }

        //Get Json data and deserialize using generics
        public static async Task<T> LoadWebData<T>(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    T result = JsonConvert.DeserializeObject<T>(jsonResponse);

                    return result;
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"Request error: {e.Message}");
                    return default;
                }
                catch (JsonException e)
                {
                    Console.WriteLine($"JSON error: {e.Message}");
                    return default;
                }
            }
        }

        //Create Connection String
        public static string getSQLConnectionStr(string dataSource, string userID, string password, string dataBase)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder
            {
                DataSource = dataSource,
                UserID = userID,
                Password = password,
                InitialCatalog = dataBase,
                TrustServerCertificate = true
            };
            return builder.ConnectionString;
        }

        //Find customer in list by acc number
        public static Customer GetCustomer(List<Customer> customerList, string accountNumber)
        {
            int accIndex = -1;
            Customer customer = null;
            foreach (var c in customerList)
            {
                if (accIndex == -1)
                {
                    accIndex = c.accounts.FindIndex(account => account.accountNumber == Int32.Parse(accountNumber));
                    customer = c;
                }
            }
            return customer;
        }
        //Validate acc num input is exists and belongs to customer
        public static int ValidateOwnAccInput(List<Customer> customerList, string? accNumber, Customer customer)
        {
            //Get Account Number
            int accIndex = -1;
            int value;

            Customer testCustomer = null;

            if (!int.TryParse(accNumber, out value))
            {
                return -2;
            }
            foreach (var c in customerList)
            {
                if (accIndex == -1)
                {
                    accIndex = c.accounts.FindIndex(account => account.accountNumber == Int32.Parse(accNumber));
                    testCustomer = c;
                }
            }
            if (accNumber.Length != 4)
            {
                return -3;
            }
            if (accIndex == -1)
            {
                return -1;
            }
            if (customer.customerID != testCustomer.customerID)
            {
                return -4;
            }
            return accIndex;
        }
        //Validate acc num input is exists and is different to transferring acc num
        public static int ValidateTransferAccInput(List<Customer> customerList, string? accNumber, string? accNumber2)
        {
            //Get Account Number
            int accIndex = -1;
            int value;

            if (!int.TryParse(accNumber2, out value))
            {
                return -2;
            }
            foreach (var c in customerList)
            {
                if (accIndex == -1)
                {
                    accIndex = c.accounts.FindIndex(account => account.accountNumber == Int32.Parse(accNumber2));
                }
            }
            if (accNumber2.Length != 4)
            {
                return -3;
            }
            if (accIndex == -1)
            {
                return -1;
            }
            if (accNumber == accNumber2)
            {
                return -4;
            }
            return accIndex;
        }

        //Make and match password to password hash
        public static bool CheckPassword(Customer customer, string password)
        {
            string passwordHash = customer.login.passwordHash;
            return new SimpleHash().Verify(password, passwordHash);
        }

        // Check menu input is in and between 0-6
        public static int ValidateMenuInput(string input)
        {
            int value;
            if (!int.TryParse(input, out value))
            {
                return -1;
            }
            if (Int32.Parse(input) <= 0 || Int32.Parse(input) > 6)
            {
                return 0;
            }
            return Int32.Parse(input);
        }

        // Check loginID input Exists and is valid
        public static int ValidateLoginID(List<Customer> customerList, string loginID)
        {
            int n;
            if (int.TryParse(loginID, out n))
            {
                if (loginID.Length == 8)
                {
                    int index = customerList.FindIndex(customer => customer.login.loginID == loginID);

                    return index;
                }
                else
                {
                    return -3;
                }
            }
            else
            {
                return -2;
            }
        }

        //Ensure input is in format 0.00 and more than 0
        public static bool IsMoney(string amount)
        {
            string[] parts = amount.Split('.');

            if (parts.Length == 2)
            {
                if (parts[1].Length == 2)
                {
                    float floatValue;
                    if (float.TryParse(amount, out floatValue))
                    {
                        if (float.Parse(amount) > 0)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static void PrintCustomerList(List<Customer> customerList)
        {
            foreach (var customer in customerList)
            {
                Debug.WriteLine("name" + customer.name);
                Console.WriteLine("id" + customer.customerID);
                Console.WriteLine("address" + customer.address);
                Console.WriteLine("city" + customer.city);
                Console.WriteLine("postCode" + customer.postCode);
                foreach (var account in customer.accounts)
                {
                    Console.WriteLine("account number" + account.accountNumber);
                    Console.WriteLine("type" + account.accountType);
                    Console.WriteLine("cus id" + account.customerID);
                    Console.WriteLine("balance" + account.balance);
                    foreach (var transaction in account.transactions)
                    {
                        Console.WriteLine("transaction id" + transaction.transactionID);
                        Console.WriteLine("type" + transaction.transactionType);
                        Console.WriteLine("account number " + transaction.accountNumber);
                        Console.WriteLine("destination" + transaction.destinationAccountNumber);
                        Console.WriteLine("amount" + transaction.amount);
                        Console.WriteLine("comment" + transaction.comment);
                        Console.WriteLine("transaction time" + transaction.transactionTimeUtc);
                    }
                }

                Console.WriteLine("loginID" + customer.login.loginID);
                Console.WriteLine("id" + customer.login.customerID);
                Console.WriteLine("hash" + customer.login.passwordHash);
            }
        }
    }

    //Customer class, stores customer, accounts, and login
    public class Customer
    {
        public required int customerID { get; set; }
        public required string name { get; set; }
        public string? address { get; set; }
        public string? city { get; set; }
        public int? postCode { get; set; }
        public List<Account> accounts { get; set; }
        public Login login { get; set; }

        //Insert this customer into table
        public void InsertRow(SqlConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText =
                "insert into Customer (CustomerID, Name, Address, City, PostCode) values (@customerID, @name, @address, @city, @postCode)";

            command.Parameters.AddWithValue("customerID", this.customerID);
            command.Parameters.AddWithValue("name", this.name);

            if (this.address != null)
            {
                command.Parameters.AddWithValue("address", this.address);
            }
            else
            {
                command.Parameters.AddWithValue("address", DBNull.Value);
            }
            if (this.city != null)
            {
                command.Parameters.AddWithValue("city", this.city);
            }
            else
            {
                command.Parameters.AddWithValue("city", DBNull.Value);
            }
            if (this.postCode != null)
            {
                command.Parameters.AddWithValue("postCode", this.postCode);
            }
            else
            {
                command.Parameters.AddWithValue("postCode", DBNull.Value);
            }

            command.ExecuteNonQuery();
        }
        private readonly ILoginService _loginService;
        public Customer(int customID, string nam, Login logi, ILoginService loginService)
        {
            customerID = customID;
            name = nam;
            login = logi;
            _loginService = loginService;
        }
        public void UpdateLogin(string newLoginID, string newLoginPassword, int newCustomerID)
        {
            _loginService.UpdateLogin(login, newLoginID, newLoginPassword, newCustomerID);
        }
    }

    //Account stores account info and transaction list
    public class Account
    {
        public required int accountNumber { get; set; }
        public required string accountType { get; set; }
        public required int customerID { get; set; }
        public required float balance { get; set; }
        public List<Transaction>? transactions { get; set; }


        //Insert this account into table
        public void InsertRow(SqlConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText =
                "insert into Account (AccountNumber, AccountType, CustomerID, Balance) values (@accountNumber, @accountType, @customerID, @balance)";
            command.Parameters.AddWithValue("accountNumber", this.accountNumber);
            command.Parameters.AddWithValue("accountType", this.accountType);
            command.Parameters.AddWithValue("customerID", this.customerID);
            command.Parameters.AddWithValue("Balance", this.balance);
            command.ExecuteNonQuery();
        }
    }

    public class Transaction
    {
        public required int transactionID { get; set; }
        public required string transactionType { get; set; }
        public required int accountNumber { get; set; }
        public int? destinationAccountNumber { get; set; }
        public required float amount { get; set; }
        public string? comment { get; set; }
        public required string transactionTimeUtc { get; set; }

        //Insert this transaction into table
        public void InsertRow(SqlConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText =
                "insert into [Transaction] (TransactionType, AccountNumber, DestinationAccountNumber, Amount, Comment, TransactionTimeUtc) values (@transactionType, @AccountNumber, @DestinationAccountNumber, @Amount, @Comment, @TransactionTimeUtc)";
            command.Parameters.AddWithValue("transactionType", this.transactionType);
            command.Parameters.AddWithValue("accountNumber", this.accountNumber);
            if (this.destinationAccountNumber != null)
            {
                command.Parameters.AddWithValue("destinationAccountNumber", this.destinationAccountNumber);
            }
            else
            {
                command.Parameters.AddWithValue("destinationAccountNumber", DBNull.Value);
            }
            command.Parameters.AddWithValue("amount", this.amount);
            if (this.comment != null)
            {
                command.Parameters.AddWithValue("comment", this.comment);
            }
            else
            {
                command.Parameters.AddWithValue("comment", DBNull.Value);
            }
            command.Parameters.AddWithValue("transactionTimeUtc", this.transactionTimeUtc);
            command.ExecuteNonQuery();
        }
    }

    //create interface
    public interface ILoginService
    {
        void UpdateLogin(Login login, string newLoginID, string newLoginPassword, int customerID);
    }

    //create login service
    public class LoginService : ILoginService
    {
        public void UpdateLogin(Login login, string newLoginID, string newLoginPassword, int customerID)
        {
            login.loginID = newLoginID;
            login.passwordHash = newLoginPassword;
            login.customerID = customerID;
        }
    }

    //Login class stores login info
    public class Login
    {
        public string loginID { get; set; }
        public int customerID { get; set; }
        public string passwordHash { get; set; }

        //Insert this login object into table
        public void InsertRow(SqlConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText =
                "insert into Login (LoginID, CustomerID, PasswordHash) values (@loginID, @customerID, @passwordHash)";
            command.Parameters.AddWithValue("loginID", this.loginID);
            command.Parameters.AddWithValue("customerID", this.customerID);
            command.Parameters.AddWithValue("passwordHash", this.passwordHash);
            command.ExecuteNonQuery();
        }
    }
}
