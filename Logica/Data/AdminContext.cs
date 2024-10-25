using Logica.Objets;
using Logica.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;  // Necesario para ODP.NET
using System;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;

public class AdminContext : DbContext
{
    private readonly string _connectionString;

    public AdminContext(DbContextOptions<AdminContext> options, IConfiguration configuration) : base(options) {
        _connectionString = configuration.GetConnectionString("OracleDbConnection")!;
    }

    


    //======================================
    //====ADMINISTRACION DE TABLESPACES=====
    //======================================

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
    NVL(CASE 
            WHEN t.contents = 'TEMPORARY' THEN tf.total_space_mb 
            ELSE df.total_space_mb 
        END, 0) AS espacio_total_mb,
    NVL(CASE 
            WHEN t.contents = 'TEMPORARY' THEN tf.total_space_mb 
            ELSE fs.free_space_mb 
        END, 0) AS espacio_libre_mb,
    NVL(CASE 
            WHEN t.contents = 'TEMPORARY' THEN (tf.total_space_mb - tf.used_space_mb)
            ELSE (df.total_space_mb - fs.free_space_mb) 
        END, 0) AS espacio_usado_mb
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
LEFT JOIN 
    (SELECT tablespace_name, 
            SUM(bytes) / 1024 / 1024 AS total_space_mb,
            SUM(bytes - blocks * 8192) / 1024 / 1024 AS used_space_mb
     FROM 
            dba_temp_files 
     GROUP BY 
            tablespace_name) tf 
ON t.tablespace_name = tf.tablespace_name
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



    //======================================
    //========SEGURIDAD DE USUARIOS=========
    //======================================



    public async Task<List<UsuarioDto>> ListarUsuariosAsync()
    {
        var usuarios = new List<UsuarioDto>();

        using (var connection = new OracleConnection(_connectionString))
        {
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT 
                    u.username AS nombre_usuario,
                    u.account_status AS estado_usuario,
                    u.default_tablespace AS tablespace_defecto,
                    u.profile AS perfil,
                    r.granted_role AS nombre_rol
                FROM 
                    dba_users u
                LEFT JOIN 
                    dba_role_privs r ON u.username = r.grantee
                WHERE 
                    u.username NOT IN ('SYS', 'SYSTEM', 'DBSNMP', 'OUTLN', 'XS$NULL', 'ORDDATA', 'ORDPLUGINS',
                                       'ORDDATA', 'ORACLE_OCM', 'CTXSYS', 'MDSYS', 'WMSYS', 'EXFSYS', 'XDB', 
                                       'ANONYMOUS', 'OLAPSYS', 'SI_INFORMTN_SCHEMA', 'SYSMAN', 'FLOWS_FILES',
                                       'APEX_040000', 'SPATIAL_CSW_ADMIN_USR', 'SPATIAL_WFS_ADMIN_USR', 
                                       'APPQOSSYS', 'DVSYS', 'GSMADMIN_INTERNAL', 'GGSYS', 'AUDSYS', 'OJVMSYS', 'SYSBACKUP',
                                       'SYSDG', 'SYSKM', 'SYSRAC', 'SYS$UMF', 'DBSFWUSER', 'DGPDB_INT', 'DIP',
                                       'DVF', 'GSMCATUSER', 'GSMROOTUSER', 'GSMUSER', 'LBACSYS', 'MDDATA', 'ORDSYS',
                                       'REMOTE_SCHEDULER_AGENT')
                ORDER BY 
                    u.username";

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var usuario = new UsuarioDto
                    {
                        NombreUsuario = reader["nombre_usuario"].ToString()!,
                        EstadoUsuario = reader["estado_usuario"].ToString()!,
                        TablespaceDefecto = reader["tablespace_defecto"].ToString()!,
                        Perfil = reader["perfil"].ToString()!,
                        NombreRol = reader["nombre_rol"]?.ToString()! // Puede ser null
                    };
                    usuarios.Add(usuario);
                }
            }
        }

        return usuarios;
    }



    public async Task<List<RoleDto>> ListarRolesAsync()
    {
        var roles = new List<RoleDto>();

        using (var connection = new OracleConnection(_connectionString))
        {
            await connection.OpenAsync();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT 
                    r.role AS nombre_rol,
                    (SELECT COUNT(*) FROM dba_role_privs ur WHERE ur.granted_role = r.role) AS numero_usuarios
                FROM 
                    dba_roles r
                WHERE 
                    r.role NOT IN (
                        'SYS', 'SYSTEM', 'DBA', 'CONNECT', 'RESOURCE', 'EXP_FULL_DATABASE', 
                        'IMP_FULL_DATABASE', 'SELECT_CATALOG_ROLE', 'EXECUTE_CATALOG_ROLE', 
                        'ANALYZE_CATALOG_ROLE', 'APEX_040000', 'APEX_PUBLIC_USER', 
                        'MDSYS', 'CTXSYS', 'ORDDATA', 'ORDDATA_AUDIT', 'OLAPSYS', 
                        'OLAP_DBA', 'SI_INFORMTN_SCHEMA', 'FLOWS_FILES', 
                        'WMSYS', 'XDB', 'DBMS_SCHEDULER', 'DBMS_AQ', 
                        'DBMS_JOB', 'DBMS_LOB', 'DBMS_OUTPUT', 'DBMS_SCHEDULER_ADMIN',
                        -- Roles a excluir
                        'ACCHK_READ', 'ADM_PARALLEL_EXECUTE_TASK', 'APPLICATION_TRACE_VIEWER',
                        'AQ_ADMINISTRATOR_ROLE', 'AQ_USER_ROLE', 'AUDIT_ADMIN', 
                        'AUDIT_VIEWER', 'AUTHENTICATEDUSER', 'AVTUNE_PKG_ROLE', 
                        'BDSQL_ADMIN', 'BDSQL_USER', 'CAPTURE_ADMIN', 'CDB_DBA', 
                        'CTXAPP', 'DATAPATCH_ROLE', 'DATAPUMP_EXP_FULL_DATABASE', 
                        'DATAPUMP_IMP_FULL_DATABASE', 'DBFS_ROLE', 'DBJAVASCRIPT', 
                        'DBMS_MDX_INTERNAL', 'DV_ACCTMGR', 'DV_ADMIN', 'DV_AUDIT_CLEANUP', 
                        'DV_DATAPUMP_NETWORK_LINK', 'DV_GOLDENGATE_ADMIN', 
                        'DV_GOLDENGATE_REDO_ACCESS', 'DV_MONITOR', 'DV_OWNER', 
                        'DV_PATCH_ADMIN', 'DV_POLICY_OWNER', 'DV_SECANALYST', 
                        'DV_STREAMS_ADMIN', 'DV_XSTREAM_ADMIN', 'EJBCLIENT', 
                        'EM_EXPRESS_ALL', 'EM_EXPRESS_BASIC', 'GATHER_SYSTEM_STATISTICS', 
                        'GDS_CATALOG_SELECT', 'GGSYS_ROLE', 'GLOBAL_AQ_USER_ROLE', 
                        'GSMADMIN_ROLE', 'GSM_POOLADMIN_ROLE', 'GSMROOTUSER_ROLE', 
                        'GSMUSER_ROLE', 'HS_ADMIN_EXECUTE_ROLE', 'HS_ADMIN_ROLE', 
                        'HS_ADMIN_SELECT_ROLE', 'JAVA_ADMIN', 'JAVADEBUGPRIV', 
                        'JAVAIDPRIV', 'JAVASYSPRIV', 'JAVAUSERPRIV', 'JMXSERVER', 
                        'LBAC_DBA', 'LOGSTDBY_ADMINISTRATOR', 'MAINTPLAN_APP', 
                        'OEM_ADVISOR', 'OEM_MONITOR', 'OLAP_USER', 'OLAP_XS_ADMIN', 
                        'OPTIMIZER_PROCESSING_RATE', 'ORDADMIN', 'PDB_DBA', 'PPLB_ROLE', 
                        'PROVISIONER', 'RDFCTX_ADMIN', 'RECOVERY_CATALOG_OWNER', 
                        'RECOVERY_CATALOG_OWNER_VPD', 'RECOVERY_CATALOG_USER', 
                        'SCHEDULER_ADMIN', 'SODA_APP', 'WM_ADMIN_ROLE', 
                        'XDBADMIN', 'XDB_SET_INVOKER', 'XDB_WEBSERVICES', 
                        'XDB_WEBSERVICES_OVER_HTTP', 'XDB_WEBSERVICES_WITH_PUBLIC', 
                        'XS_CACHE_ADMIN', 'XS_CONNECT', 'XS_NAMESPACE_ADMIN', 
                        'XS_SESSION_ADMIN', 'SYSUMF_ROLE'
                    )
                ORDER BY 
                    r.role";

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var role = new RoleDto
                    {
                        NombreRol = reader["nombre_rol"]?.ToString()!, // Puede ser null
                        NumeroUsuarios = reader["numero_usuarios"] != DBNull.Value ? reader.GetInt32(reader.GetOrdinal("numero_usuarios")) : 0 // Obtener como entero
                    };
                    roles.Add(role);
                }
            }
        }

        return roles;
    }







}

