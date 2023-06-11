using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebAPIGestionBancaire.Models
{
    public class Compte
    {
        public int ID_compte { get; set; }
        public string Nom { get; set; }
        public decimal Solde { get; set; }
    }

}
