using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnacksApp.Models
{
    public class ImagemPerfil
    {
        public string? UrlImagem { get; set; }
        
        public string? CaminhoImagem => "https://nmbd2wm8-7066.uks1.devtunnels.ms/"+UrlImagem;
    }
}
