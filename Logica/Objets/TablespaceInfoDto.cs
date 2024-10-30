using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica.Objets
{
    public class TablespaceInfoDto
    {
        public string TablespaceName { get; set; } = string.Empty;
        public decimal UsedMb { get; set; }
        public decimal TotalMb { get; set; }
        public decimal FreeMb { get; set; }
        public decimal PctUsed { get; set; }
    }
}
