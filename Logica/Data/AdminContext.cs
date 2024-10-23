using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;  // Necesario para ODP.NET
using System;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;

public class AdminContext : DbContext
{
    public AdminContext(DbContextOptions<AdminContext> options) : base(options) { }

    public AdminContext() { }



    public virtual async Task<List<string>> ListarTablespacesAsync()
    {
        var query = "SELECT tablespace_name FROM dba_tablespaces";

        var connection = Database.GetDbConnection();
        await connection.OpenAsync();

        try
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                command.CommandType = CommandType.Text;

                using (var reader = await command.ExecuteReaderAsync())
                {
                    var tablespaces = new List<string>();

                    while (await reader.ReadAsync())
                    {
                        tablespaces.Add(reader.GetString(0));
                    }

                    return tablespaces; // Devuelve la lista directamente
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error de conexión a la base de datos: " + ex.Message);
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

}

