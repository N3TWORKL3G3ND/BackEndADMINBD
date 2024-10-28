using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace Logica.Services
{
    public class FileService
    {
        private readonly string _directorioRespaldos;

        public FileService()
        {
            _directorioRespaldos = "C:\\ADMINBD\\RESPALDOS";
        }



        public virtual async Task<List<string>> ListarArchivosDmpAsync()
        {
            List<string> archivosDmp = new List<string>();

            // Utiliza Task.Run para realizar la operación de I/O en un hilo diferente
            await Task.Run(() =>
            {
                // Verifica si el directorio existe
                if (Directory.Exists(_directorioRespaldos))
                {
                    // Obtiene los archivos .dmp en el directorio
                    string[] archivos = Directory.GetFiles(_directorioRespaldos, "*.dmp");

                    // Agrega los nombres de los archivos a la lista
                    foreach (string archivo in archivos)
                    {
                        archivosDmp.Add(Path.GetFileName(archivo)); // Solo el nombre del archivo
                    }
                }
            });

            return archivosDmp;
        }






    }
}
