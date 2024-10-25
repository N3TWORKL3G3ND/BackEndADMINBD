using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica.Objets
{
    public class UsuarioDto
    {
        public string NombreUsuario { get; set; } = string.Empty;
        public string EstadoUsuario { get; set; } = string.Empty;
        public string TablespaceDefecto { get; set; } = string.Empty;
        public string Perfil { get; set; } = string.Empty;
        public string NombreRol { get; set; } = string.Empty;
    }
}
