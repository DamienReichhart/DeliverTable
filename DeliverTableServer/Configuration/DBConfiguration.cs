using DeliverTableServer.Data;

namespace DeliverTableServer.Configuration;

public class DBConfiguration
{
    public static string BuildConnectionString()
    {
        return Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? "";
    }

    public static void VerifyConnectionToDB(IServiceProvider serviceProvider)
    {
        using (var scope = serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DeliverTableContext>();
            try
            {
                if (db.Database.CanConnect())
                    Console.WriteLine("✅ La base de données PostgreSQL est accessible !");
                else
                    Console.WriteLine("❌ Impossible de se connecter à la base de données !");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Erreur de connexion : " + ex.Message);
            }
        }
    }
}