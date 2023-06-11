using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebAPIGestionBancaire.Models
{
    public class Virement
    {

        public int idCompteSource { get; set; }
        public int idCompteDestinataire { get; set; }
        
        public decimal montant { get; set; }
    }
}
