﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logica.Requests
{
    public class ReqRecResFull
    {
        [Required(ErrorMessage = "El nombre del archivo de respaldo es obligatorio.")]
        public string nombreArchivoRespaldo { get; set; } = string.Empty;
    }
}