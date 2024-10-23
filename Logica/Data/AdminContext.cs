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



    public async Task<bool> EliminarTablespaceAsync(string nombre)
    {
        var query = $"DROP TABLESPACE {nombre} INCLUDING CONTENTS AND DATAFILES";

        using (var connection = Database.GetDbConnection())
        {
            await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                command.CommandType = CommandType.Text;

                try
                {
                    await command.ExecuteNonQueryAsync();
                    // Si no se lanza una excepción, se considera que se eliminó correctamente
                    return true;
                }
                catch (OracleException ex) // Captura la excepción de Oracle
                {
                    // Verifica si el error es que el tablespace no existe
                    if (ex.Number == 959) // ORA-00959
                    {
                        throw new Exception($"Error al eliminar el tablespace: El tablespace '{nombre}' no existe.");
                    }

                    // Si es otro error, relanza la excepción original
                    throw new Exception("Error al eliminar el tablespace: " + ex.Message);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error al eliminar el tablespace: " + ex.Message);
                }
            }
        }
    }



}

