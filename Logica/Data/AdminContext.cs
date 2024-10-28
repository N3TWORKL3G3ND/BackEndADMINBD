using Logica.Objets;
using Logica.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;  // Necesario para ODP.NET
using System;
using System.Data;
using System.Diagnostics;
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



    public virtual async Task<List<TablespaceDto>> ListarTablespacesConDetallesAsync()
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
                    var tablespacesConDetalles = new List<TablespaceDto>();

                    while (await reader.ReadAsync())
                    {
                        var detalle = new TablespaceDto
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



    public virtual async Task<string> CrearUsuarioAsync(string nombre, string contrasenna, string nombreTablespace, string nombreTablespaceTemporal, string nombreRol)
    {
        // Conexión a la base de datos
        using (var connection = new OracleConnection(_connectionString))
        {
            await connection.OpenAsync();

            try
            {
                // Verificar si el tablespace existe
                if (!await VerificarTablespaceAsync(connection, nombreTablespace))
                {
                    return $"Error: El tablespace '{nombreTablespace}' no existe.";
                }

                if (!await VerificarTablespaceAsync(connection, nombreTablespaceTemporal))
                {
                    return $"Error: El tablespace temporal '{nombreTablespaceTemporal}' no existe.";
                }

                // Verificar si el rol existe
                if (!await VerificarRolAsync(connection, nombreRol))
                {
                    return $"Error: El rol '{nombreRol}' no existe.";
                }

                // Alterar la sesion
                var command = connection.CreateCommand();
                command.CommandText = $@"
                ALTER SESSION SET ""_ORACLE_SCRIPT"" = TRUE";

                await command.ExecuteNonQueryAsync();

                // Crear el usuario
                command = connection.CreateCommand();
                command.CommandText = $@"
                CREATE USER {nombre} IDENTIFIED BY {contrasenna}
                DEFAULT TABLESPACE {nombreTablespace}
                TEMPORARY TABLESPACE {nombreTablespaceTemporal}";

                await command.ExecuteNonQueryAsync();

                // Asignar el rol al usuario
                command.CommandText = $"GRANT {nombreRol} TO {nombre}";
                await command.ExecuteNonQueryAsync();

                return $"Usuario '{nombre}' creado exitosamente.";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }



    public virtual async Task<string> EliminarUsuarioAsync(string nombreUsuario)
    {
        using (var connection = new OracleConnection(_connectionString))
        {
            await connection.OpenAsync();

            try
            {
                // Verificar si el usuario existe
                if (!await VerificarUsuarioAsync(connection, nombreUsuario))
                {
                    return $"Error: El usuario '{nombreUsuario}' no existe.";
                }

                // Alterar la sesion
                var command = connection.CreateCommand();
                command.CommandText = $@"
                ALTER SESSION SET ""_ORACLE_SCRIPT"" = TRUE";
                await command.ExecuteNonQueryAsync();

                // Revoke roles first
                var revokeRolesCommand = connection.CreateCommand();
                revokeRolesCommand.CommandText = @"
                SELECT granted_role 
                FROM dba_role_privs 
                WHERE grantee = :nombreUsuario";
                revokeRolesCommand.Parameters.Add(new OracleParameter("nombreUsuario", nombreUsuario));

                using (var reader = await revokeRolesCommand.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var role = reader.GetString(0);
                        var revokeRoleCommand = connection.CreateCommand();
                        revokeRoleCommand.CommandText = $"REVOKE \"{role}\" FROM \"{nombreUsuario}\"";
                        await revokeRoleCommand.ExecuteNonQueryAsync();
                    }
                }

                // Now attempt to drop the user
                var dropUserCommand = connection.CreateCommand();
                dropUserCommand.CommandText = $"DROP USER \"{nombreUsuario}\" CASCADE";
                await dropUserCommand.ExecuteNonQueryAsync();

                return $"Usuario '{nombreUsuario}' eliminado exitosamente.";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }



    public virtual async Task<string> CrearRolAsync(string nombreRol)
    {
        using (var connection = new OracleConnection(_connectionString))
        {
            await connection.OpenAsync();

            try
            {
                // Verificar si el rol ya existe
                var rolExistenteCommand = connection.CreateCommand();
                rolExistenteCommand.CommandText = @"
                SELECT COUNT(*) 
                FROM dba_roles 
                WHERE role = :nombreRol";
                rolExistenteCommand.Parameters.Add(new OracleParameter("nombreRol", nombreRol));

                var rolExistente = await rolExistenteCommand.ExecuteScalarAsync();

                if (Convert.ToInt32(rolExistente) > 0)
                {
                    return $"Error: El rol '{nombreRol}' ya existe.";
                }

                // Alterar la sesion
                var command = connection.CreateCommand();
                command.CommandText = $@"
                ALTER SESSION SET ""_ORACLE_SCRIPT"" = TRUE";
                await command.ExecuteNonQueryAsync();

                // Crear el rol
                var crearRolCommand = connection.CreateCommand();
                crearRolCommand.CommandText = $"CREATE ROLE \"{nombreRol}\"";
                await crearRolCommand.ExecuteNonQueryAsync();

                // Otorgar privilegios al rol
                var grantAllPrivilegesCommand = connection.CreateCommand();
                grantAllPrivilegesCommand.CommandText = $"GRANT ALL PRIVILEGES TO \"{nombreRol}\"";
                await grantAllPrivilegesCommand.ExecuteNonQueryAsync();

                var grantCreateSessionCommand = connection.CreateCommand();
                grantCreateSessionCommand.CommandText = $"GRANT CREATE SESSION TO \"{nombreRol}\"";
                await grantCreateSessionCommand.ExecuteNonQueryAsync();

                return $"Rol '{nombreRol}' creado exitosamente con privilegios otorgados.";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }



    public virtual async Task<string> EliminarRolAsync(string nombreRol)
    {
        using (var connection = new OracleConnection(_connectionString))
        {
            await connection.OpenAsync();

            try
            {
                // Verificar si el rol existe
                var checkCommand = connection.CreateCommand();
                checkCommand.CommandText = $"SELECT COUNT(*) FROM ROLE_SYS_PRIVS WHERE ROLE = :rol";
                checkCommand.Parameters.Add(new OracleParameter("rol", nombreRol));

                var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

                if (count == 0)
                {
                    return $"Error: El rol '{nombreRol}' no existe.";
                }

                // Alterar la sesion
                var command = connection.CreateCommand();
                command.CommandText = $@"
                ALTER SESSION SET ""_ORACLE_SCRIPT"" = TRUE";
                await command.ExecuteNonQueryAsync();

                // Crear el comando para eliminar el rol
                command = connection.CreateCommand();
                command.CommandText = $"DROP ROLE \"{nombreRol}\"";
                await command.ExecuteNonQueryAsync();

                return $"Rol '{nombreRol}' eliminado exitosamente.";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }



    //======================================
    //=============RESPALDOS================
    //======================================



    public virtual async Task<string> RespaldarEsquemaAsync(string nombreEsquema)
    {
        // Generar un nombre preferente para el respaldo con fecha y hora
        string nombreRespaldo = $"{nombreEsquema}_Respaldo_{DateTime.Now:yyyyMMdd_HHmmss}";

        // Define la ruta donde se almacenará el respaldo
        string rutaRespaldo = $@"C:\ADMINBD\RESPALDOS\{nombreRespaldo}";

        // Crea el comando para ejecutar el respaldo
        string comando = $@"expdp 'SYS/root123@localhost:1521/XE AS SYSDBA' schemas={nombreEsquema} directory=DATA_PUMP_DIR dumpfile={nombreRespaldo}.dmp logfile={nombreRespaldo}.log";



        try
        {
            // Crea un proceso para ejecutar el comando expdp
            var proceso = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $@"/C {comando}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            // Inicia el proceso
            proceso.Start();

            // Lee la salida del proceso
            string salida = await proceso.StandardOutput.ReadToEndAsync();
            string errores = await proceso.StandardError.ReadToEndAsync();

            // Espera a que el proceso finalice
            await proceso.WaitForExitAsync();

            // Verifica si hubo errores
            if (proceso.ExitCode != 0)
            {
                return $"Error: {errores}";
            }

            return $"Respaldo del esquema '{nombreEsquema}' creado exitosamente en '{rutaRespaldo}'.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }



    public virtual async Task<string> RespaldarFullAsync()
    {
        // Generar un nombre preferente para el respaldo con fecha y hora
        string nombreRespaldo = $"FULL_Respaldo_{DateTime.Now:yyyyMMdd_HHmmss}";

        // Define la ruta donde se almacenará el respaldo
        string rutaRespaldo = $@"C:\ADMINBD\RESPALDOS\{nombreRespaldo}";

        // Crea el comando para ejecutar el respaldo
        string comando = $@"expdp 'SYS/root123@localhost:1521/XE AS SYSDBA' FULL=Y directory=DATA_PUMP_DIR dumpfile={nombreRespaldo}.dmp logfile={nombreRespaldo}.log";



        try
        {
            // Crea un proceso para ejecutar el comando expdp
            var proceso = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $@"/C {comando}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            // Inicia el proceso
            proceso.Start();

            // Lee la salida del proceso
            string salida = await proceso.StandardOutput.ReadToEndAsync();
            string errores = await proceso.StandardError.ReadToEndAsync();

            // Espera a que el proceso finalice
            await proceso.WaitForExitAsync();

            // Verifica si hubo errores
            if (proceso.ExitCode != 0)
            {
                return $"Error: {errores}";
            }

            return $"Respaldo COMPLETO de la base de datos fue creado exitosamente en '{rutaRespaldo}'.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }



    public virtual async Task<List<string>> ListarTablasDeEsquemaAsync(string nombreEsquema)
    {
        List<string> tablas = new List<string>();

        string comando = $@"SELECT table_name FROM all_tables WHERE owner = '{nombreEsquema.ToUpper()}'";

        using (var connection = new OracleConnection(_connectionString))
        {
            await connection.OpenAsync(); // Versión asíncrona para abrir la conexión

            using (var command = new OracleCommand(comando, connection))
            {
                using (var reader = await command.ExecuteReaderAsync()) // Versión asíncrona del lector
                {
                    while (await reader.ReadAsync()) // Versión asíncrona para leer
                    {
                        tablas.Add(reader.GetString(0)); // Obtiene el nombre de la tabla
                    }
                }
            }
        }

        return tablas;
    }



    public virtual async Task<string> RespaldarTablaAsync(string nombreEsquema, string nombreTabla)
    {
        // Generar un nombre preferente para el respaldo con fecha y hora
        string nombreRespaldo = $"Respaldo_{nombreEsquema}_{nombreTabla}_{DateTime.Now:yyyyMMdd_HHmmss}";

        // Define la ruta donde se almacenará el respaldo
        string rutaRespaldo = $@"C:\ADMINBD\RESPALDOS\{nombreRespaldo}";

        // Crea el comando para ejecutar el respaldo
        string comando = $@"expdp 'SYS/root123@localhost:1521/XE AS SYSDBA' tables={nombreEsquema}.{nombreTabla} directory=DATA_PUMP_DIR dumpfile={nombreRespaldo}.dmp logfile={nombreRespaldo}.log";

        try
        {
            // Crea un proceso para ejecutar el comando expdp
            var proceso = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $@"/C {comando}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            // Inicia el proceso
            proceso.Start();

            // Lee la salida del proceso
            string salida = await proceso.StandardOutput.ReadToEndAsync();
            string errores = await proceso.StandardError.ReadToEndAsync();

            // Espera a que el proceso finalice
            await proceso.WaitForExitAsync();

            // Verifica si hubo errores
            if (proceso.ExitCode != 0)
            {
                return $"Error: {errores}";
            }

            return $"Respaldo de la tabla '{nombreTabla}' en el esquema '{nombreEsquema}' fue creado exitosamente en '{rutaRespaldo}'.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }



    public virtual async Task<string> RecuperarRespaldoEsquemaAsync(string nombreEsquema, string nombreRespaldo)
    {
        // Define la ruta donde se encuentra el respaldo
        string rutaRespaldo = $@"C:\ADMINBD\RESPALDOS\{nombreRespaldo}";

        // Comando para ejecutar la recuperación del respaldo
        string comandoRecuperacion = $@"impdp 'SYS/root123@localhost:1521/XE AS SYSDBA' schemas={nombreEsquema} directory=DATA_PUMP_DIR dumpfile={nombreRespaldo} logfile=RECUPERACION_{nombreEsquema}.log";

        
        try
        {
            // Verificar y crear el usuario si es necesario
            await VerificarYCrearEsquemaAsync(nombreEsquema);

            // Crea un proceso para ejecutar el comando impdp
            var proceso = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $@"/C {comandoRecuperacion}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            // Inicia el proceso
            proceso.Start();

            // Lee la salida del proceso
            string salida = await proceso.StandardOutput.ReadToEndAsync();
            string errores = await proceso.StandardError.ReadToEndAsync();

            // Espera a que el proceso finalice
            await proceso.WaitForExitAsync();

            // Verifica si hubo errores
            if (proceso.ExitCode != 0)
            {
                return $"Error: {errores}";
            }

            return $"El esquema '{nombreEsquema}' fue recuperado exitosamente desde el respaldo '{nombreRespaldo}.dmp'.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }



    public virtual async Task<string> RecuperarRespaldoTablaAsync(string nombreTabla, string nombreRespaldo)
    {
        // Define la ruta donde se encuentra el respaldo
        string rutaRespaldo = $@"C:\ADMINBD\RESPALDOS\{nombreRespaldo}";

        // Comando para ejecutar la recuperación del respaldo
        string comandoRecuperacion = $@"impdp 'SYS/password@localhost:1521/XE AS SYSDBA' directory=DATA_PUMP_DIR dumpfile={nombreRespaldo} logfile=archivo_import.log tables=PADRON.{nombreTabla} table_exists_action=replace";


        try
        {
            // Crea un proceso para ejecutar el comando impdp
            var proceso = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $@"/C {comandoRecuperacion}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            // Inicia el proceso
            proceso.Start();

            // Lee la salida del proceso
            string salida = await proceso.StandardOutput.ReadToEndAsync();
            string errores = await proceso.StandardError.ReadToEndAsync();

            // Espera a que el proceso finalice
            await proceso.WaitForExitAsync();

            // Verifica si hubo errores
            if (proceso.ExitCode != 0)
            {
                return $"Error: {errores}";
            }

            return $"La tabla '{nombreTabla}' fue recuperada exitosamente desde el respaldo '{nombreRespaldo}.dmp'.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }



    public virtual async Task<string> RecuperarRespaldoCompletoAsync(string nombreRespaldo)
    {
        // Define la ruta donde se encuentra el respaldo
        string rutaRespaldo = $@"C:\ADMINBD\RESPALDOS\{nombreRespaldo}";

        // Comando para ejecutar la recuperación del respaldo completo
        string comandoRecuperacion = $@"impdp 'SYS/password@localhost:1521/XE AS SYSDBA' directory=DATA_PUMP_DIR dumpfile={nombreRespaldo} logfile=archivo_import.log full=y table_exists_action=replace";

        try
        {
            // Crea un proceso para ejecutar el comando impdp
            var proceso = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $@"/C {comandoRecuperacion}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            // Inicia el proceso
            proceso.Start();

            // Lee la salida del proceso
            string salida = await proceso.StandardOutput.ReadToEndAsync();
            string errores = await proceso.StandardError.ReadToEndAsync();

            // Espera a que el proceso finalice
            await proceso.WaitForExitAsync();

            // Verifica si hubo errores
            if (proceso.ExitCode != 0)
            {
                return $"Error: {errores}";
            }

            return $"El respaldo completo fue recuperado exitosamente desde '{nombreRespaldo}.dmp'.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }



    //======================================
    //===============TUNNING================
    //======================================



    public virtual async Task<List<string>> ListarIndicesDeUsuarioAsync(string nombreTabla)
    {
        List<string> indices = new List<string>();

        string comando = $@"
        SELECT index_name 
        FROM user_indexes 
        WHERE table_name = '{nombreTabla.ToUpper()}'";

        using (var connection = new OracleConnection(_connectionString))
        {
            await connection.OpenAsync();

            using (var command = new OracleCommand(comando, connection))
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        indices.Add(reader.GetString(0)); // Obtiene el nombre del índice
                    }
                }
            }
        }

        return indices;
    }



    public virtual async Task<string> GenerarIndiceALAJUELAAsync()
    {
        // Comando para crear un índice en la columna CEDULA
        string comandoCrearIndice = @"
        CREATE INDEX IDX_ALAJUELA_CEDULA 
        ON PADRON.ALAJUELA (CEDULA)";

        try
        {
            using (var connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new OracleCommand(comandoCrearIndice, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }

            return "Índice creado exitosamente en la tabla ALAJUELA.";
        }
        catch (Exception ex)
        {
            return $"Error al crear el índice: {ex.Message}";
        }
    }



    public virtual async Task<string> EliminarIndiceAsync(string nombreIndice)
    {
        // Comando SQL para eliminar el índice
        string comandoEliminar = $@"DROP INDEX {nombreIndice}";

        try
        {
            using (var connection = new OracleConnection(_connectionString)) // Conexión a la base de datos
            {
                await connection.OpenAsync(); // Abre la conexión de forma asíncrona

                using (var command = new OracleCommand(comandoEliminar, connection)) // Ejecuta el comando SQL
                {
                    await command.ExecuteNonQueryAsync(); // Ejecuta el comando de eliminación
                }
            }

            return $"Índice '{nombreIndice}' eliminado exitosamente.";
        }
        catch (OracleException ex)
        {
            return $"Error al eliminar el índice: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error inesperado: {ex.Message}";
        }
    }



















    //======================================
    //==========METODOS AUXILIARES==========
    //======================================



    // Método para verificar si un tablespace existe
    private async Task<bool> VerificarTablespaceAsync(OracleConnection connection, string nombreTablespace)
    {
        var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM dba_tablespaces WHERE tablespace_name = :nombreTablespace";
        command.Parameters.Add(new OracleParameter("nombreTablespace", nombreTablespace.ToUpper()));

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }



    // Método para verificar si un rol existe
    private async Task<bool> VerificarRolAsync(OracleConnection connection, string nombreRol)
    {
        var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM dba_roles WHERE role = :nombreRol";
        command.Parameters.Add(new OracleParameter("nombreRol", nombreRol.ToUpper()));

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }



    private async Task<bool> VerificarUsuarioAsync(OracleConnection connection, string nombreUsuario)
    {
        var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM all_users WHERE username = '{nombreUsuario.ToUpper()}'";

        var result = await command.ExecuteScalarAsync();
        return (Convert.ToInt32(result) > 0);
    }



    private async Task VerificarYCrearEsquemaAsync(string nombreEsquema)
    {
        // Comando para verificar si el esquema ya existe
        string verificarEsquema = $@"SELECT COUNT(*) FROM dba_users WHERE username = '{nombreEsquema.ToUpper()}'";
        

        // Comandos para crear el esquema si no existe
        string crearTablespace = $@"CREATE TABLESPACE {nombreEsquema}_DAT DATAFILE 'C:\ADMINBD\{nombreEsquema}_DAT.dbf' SIZE 10M AUTOEXTEND ON NEXT 10M MAXSIZE 1G";
        string crearTablespaceTemporal = $@"CREATE TEMPORARY TABLESPACE {nombreEsquema}_TEMP TEMPFILE 'C:\ADMINBD\{nombreEsquema}_TEMP.dbf' SIZE 10M AUTOEXTEND ON NEXT 10M MAXSIZE 1G";
        string crearUsuario = $@"CREATE USER {nombreEsquema} IDENTIFIED BY root123 DEFAULT TABLESPACE {nombreEsquema}_DAT TEMPORARY TABLESPACE {nombreEsquema}_TEMP";
        string otorgarPermisos1 = $@"GRANT CREATE SESSION TO {nombreEsquema}";
        string otorgarPermisos2 = $@"GRANT CONNECT TO {nombreEsquema}";
        string otorgarPermisos3 = $@"GRANT ALL PRIVILEGES TO {nombreEsquema}";

        using (var connection = new OracleConnection(_connectionString))
        {
            await connection.OpenAsync();

            // Verificar si el esquema ya existe
            using (var commandVerificar = new OracleCommand(verificarEsquema, connection))
            {
                int esquemaExiste = Convert.ToInt32(await commandVerificar.ExecuteScalarAsync());
                

                if (esquemaExiste == 0)
                {
                    // Alterar la sesion
                    var command = connection.CreateCommand();
                    command.CommandText = $@"
                    ALTER SESSION SET ""_ORACLE_SCRIPT"" = TRUE";
                    await command.ExecuteNonQueryAsync();



                    // El esquema no existe, lo creamos
                    using (var commandCrearTablespace = new OracleCommand(crearTablespace, connection))
                    {
                        await commandCrearTablespace.ExecuteNonQueryAsync();
                    }

                    using (var commandCrearTablespaceTemp = new OracleCommand(crearTablespaceTemporal, connection))
                    {
                        await commandCrearTablespaceTemp.ExecuteNonQueryAsync();
                    }

                    using (var commandCrearUsuario = new OracleCommand(crearUsuario, connection))
                    {
                        await commandCrearUsuario.ExecuteNonQueryAsync();
                    }

                    using (var commandOtorgarPermisos = new OracleCommand(otorgarPermisos1, connection))
                    {
                        await commandOtorgarPermisos.ExecuteNonQueryAsync();
                    }

                    using (var commandOtorgarPermisos = new OracleCommand(otorgarPermisos2, connection))
                    {
                        await commandOtorgarPermisos.ExecuteNonQueryAsync();
                    }

                    using (var commandOtorgarPermisos = new OracleCommand(otorgarPermisos3, connection))
                    {
                        await commandOtorgarPermisos.ExecuteNonQueryAsync();
                    }
                }
                Console.WriteLine("Esquema: " + esquemaExiste);
            }
        }
    }



















}

