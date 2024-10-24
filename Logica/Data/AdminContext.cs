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
    NVL(df.total_space_mb, 0) AS espacio_total_mb,
    NVL(fs.free_space_mb, 0) AS espacio_libre_mb,
    (NVL(df.total_space_mb, 0) - NVL(fs.free_space_mb, 0)) AS espacio_usado_mb
FROM 
    dba_tablespaces t
LEFT JOIN 
    (SELECT tablespace_name, 
            SUM(bytes) / 1024 / 1024 AS total_space_mb 
     FROM 
            dba_data_files 
     GROUP BY 
            tablespace_name) df 
ON t.tablespace_name = df.tablespace_name
LEFT JOIN 
    (SELECT tablespace_name, 
            SUM(bytes) / 1024 / 1024 AS free_space_mb 
     FROM 
            dba_free_space 
     GROUP BY 
            tablespace_name) fs 
ON t.tablespace_name = fs.tablespace_name
WHERE 
    t.contents IN ('PERMANENT', 'TEMPORARY')";

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



    public async Task<string> RedimensionarTablespaceAsync(string nombre, int tamanno)
    {
        // Consultas para obtener el archivo de datos o el archivo temporal asociado con el tablespace
        var queryObtenerArchivo = @"
    SELECT file_name 
    FROM dba_data_files 
    WHERE tablespace_name = :nombre
    UNION ALL
    SELECT file_name 
    FROM dba_temp_files 
    WHERE tablespace_name = :nombre";

        var connection = Database.GetDbConnection();
        await connection.OpenAsync();

        try
        {
            // Paso 1: Obtener el nombre del archivo de datos o temporal asociado con el tablespace
            string nombreArchivo = null;
            bool esTemporal = false;

            using (var command = connection.CreateCommand())
            {
                command.CommandText = queryObtenerArchivo;
                command.CommandType = CommandType.Text;

                var parameter = command.CreateParameter();
                parameter.ParameterName = ":nombre";
                parameter.Value = nombre.ToUpper(); // Oracle usa nombres de tablas y tablespaces en mayúsculas
                command.Parameters.Add(parameter);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        nombreArchivo = reader.GetString(0); // Nombre del archivo
                                                             // Verificamos si el archivo pertenece a un tablespace temporal
                        esTemporal = reader.FieldCount == 1 && reader.GetString(0).Contains("TEMP"); // Esto es solo un ejemplo, puedes tener otro criterio
                    }
                    else
                    {
                        return $"Error: No se encontró un archivo asociado para el tablespace '{nombre}'.";
                    }
                }
            }

            if (string.IsNullOrEmpty(nombreArchivo))
            {
                return $"Error: No se encontró archivo asociado para el tablespace '{nombre}'.";
            }

            // Paso 2: Redimensionar el archivo
            string queryRedimensionar;

            // Verificar si es un tablespace temporal
            if (esTemporal)
            {
                // Para tablespaces temporales, la redimensión se realiza de manera diferente
                queryRedimensionar = $"ALTER DATABASE TEMPFILE '{nombreArchivo}' RESIZE {tamanno}M";
            }
            else
            {
                // Para tablespaces permanentes
                queryRedimensionar = $"ALTER DATABASE DATAFILE '{nombreArchivo}' RESIZE {tamanno}M";
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = queryRedimensionar;
                command.CommandType = CommandType.Text;

                var resultado = await command.ExecuteNonQueryAsync();

                // Si la consulta se ejecuta correctamente, el tamaño del archivo ha sido ajustado
                return $"El tablespace '{nombre}' ha sido redimensionado a {tamanno} MB correctamente.";
            }
        }
        catch (Exception ex)
        {
            // Manejo de excepciones
            return $"Error al redimensionar el tablespace '{nombre}': {ex.Message}";
        }
        finally
        {
            await connection.CloseAsync();
        }
    }





    public virtual async Task<string> CrearTablespaceAsync(string nombre, int tamanno)
    {
        // Sufijos para los tipos de tablespace
        string nombreData = $"{nombre}_DAT";
        string nombreTemp = $"{nombre}_TEMP";

        // Consulta para verificar si el tablespace ya existe
        var verificarQuery = $"SELECT COUNT(*) FROM dba_tablespaces WHERE tablespace_name = '{nombreData}'";

        var connection = Database.GetDbConnection();
        await connection.OpenAsync();

        try
        {
            using (var command = connection.CreateCommand())
            {
                // Verificamos si el tablespace ya existe
                command.CommandText = verificarQuery;
                command.CommandType = CommandType.Text;

                var existe = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;

                if (existe)
                {
                    return $"Error: El tablespace '{nombreData}' ya existe. No se puede crear.";
                }

                // Consulta para crear el tablespace permanente
                var crearDataQuery = $"CREATE TABLESPACE {nombreData} " +
                                     $"DATAFILE 'C:\\ADMINBD\\{nombreData}.dbf' " +
                                     $"SIZE {tamanno}M";

                // Ejecutar la creación del tablespace permanente
                command.CommandText = crearDataQuery;
                await command.ExecuteNonQueryAsync();

                // Consulta para crear el tablespace temporal
                var crearTempQuery = $"CREATE TEMPORARY TABLESPACE {nombreTemp} " +
                                     $"TEMPFILE 'C:\\ADMINBD\\{nombreTemp}.dbf' " +
                                     $"SIZE {tamanno}M";

                // Ejecutar la creación del tablespace temporal
                command.CommandText = crearTempQuery;
                await command.ExecuteNonQueryAsync();

                return $"Los tablespaces '{nombreData}' y '{nombreTemp}' han sido creados correctamente.";
            }
        }
        catch (Exception ex)
        {
            return $"Error al crear los tablespaces: {ex.Message}";
        }
        finally
        {
            await connection.CloseAsync();
        }
    }









}

