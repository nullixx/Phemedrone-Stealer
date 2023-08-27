using System.Data.SQLite;

namespace Phemedrone.Panel;

public class DatabaseWorker
{
    private SQLiteConnection _connection;
    public DatabaseWorker(string dbPath)
    {
        _connection = new SQLiteConnection("Data Source="+dbPath+";Version=3;");
        _connection.Open();
    }

    public void FirstInit()
    {
        var command = new SQLiteCommand(@"CREATE TABLE IF NOT EXISTS clients (
        country_code TEXT,
        ip TEXT,
        username TEXT,
        hwid TEXT,
        contents TEXT,
        date DATETIME DEFAULT CURRENT_TIMESTAMP);", _connection);
        command.ExecuteNonQuery();
    }

    public void AddClient(string countryCode, string ip, string username, string hwid, string contents)
    {
        var command = new SQLiteCommand("INSERT INTO clients(country_code, ip, username, hwid, contents) VALUES(@countryCode, @ip, @username, @hwid, @contents)", _connection);
        command.Parameters.AddWithValue("@countryCode", countryCode);
        command.Parameters.AddWithValue("@ip", ip);
        command.Parameters.AddWithValue("@username", username);
        command.Parameters.AddWithValue("@hwid", hwid);
        command.Parameters.AddWithValue("@contents", contents);
        command.ExecuteNonQuery();
    }

    public List<string[]> GetClients()
    {
        var cmd = new SQLiteCommand("SELECT * FROM clients", _connection); 

        var values = new List<string[]>();

        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                values.Add(new []
                {
                    (string)reader["country_code"],
                    (string)reader["ip"],
                    (string)reader["username"],
                    (string)reader["hwid"],
                    (string)reader["contents"],
                });
            }
            
        }
        return values;
    }
}