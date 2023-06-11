using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebAPIGestionBancaire.Models
{
    public class Mouvement
    {
        public int ID_mouvement { get; set; }
        public int ID_compte { get; set; }
        public int? ID_compte_destinataire { get; set; } // Nullable
        public string Type_mouvement { get; set; }
        public decimal Montant { get; set; }
        public DateTime Date { get; set; }
    }

}
