using Logica.Objets;
using Logica.Responses;
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



    public virtual async Task<List<TablespaceDetalle>> ListarTablespacesConDetallesAsync()
    {
        var query = @"
    SELECT 
        t.tablespace_name AS nombre_tablespace,
        t.status AS estado,
        t.contents AS tipo_contenido,
        df.total_space_mb AS espacio_total_mb,
        NVL(fs.free_space_mb, 0) AS espacio_libre_mb,
        (df.total_space_mb - NVL(fs.free_space_mb, 0)) AS espacio_usado_mb
    FROM 
        dba_tablespaces t
    JOIN 
        (SELECT tablespace_name, 
                SUM(bytes) / 1024 / 1024 AS total_space_mb 
         FROM dba_data_files 
         GROUP BY tablespace_name) df 
    ON t.tablespace_name = df.tablespace_name
    LEFT JOIN 
        (SELECT tablespace_name, 
                SUM(bytes) / 1024 / 1024 AS free_space_mb 
         FROM dba_free_space 
         GROUP BY tablespace_name) fs 
    ON t.tablespace_name = fs.tablespace_name";

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
                    var tablespacesConDetalles = new List<TablespaceDetalle>();

                    while (await reader.ReadAsync())
                    {
                        var detalle = new TablespaceDetalle
                        {
                            nombre = reader.GetString(0),               // Nombre del tablespace
                            estado = reader.GetString(1),               // Estado (ej. ONLINE, OFFLINE)
                            tipo = reader.GetString(2),                 // Tipo (ej. PERMANENT, TEMPORARY)
                            tamano_total_mb = reader.GetDecimal(3),     // Tamaño total (en MB)
                            tamano_libre_mb = reader.GetDecimal(4),     // Espacio libre (en MB)
                            tamano_usado_mb = reader.GetDecimal(5)      // Espacio usado (en MB)
                        };

                        tablespacesConDetalles.Add(detalle);
                    }

                    return tablespacesConDetalles;
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error al obtener los detalles de los tablespaces: " + ex.Message);
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

