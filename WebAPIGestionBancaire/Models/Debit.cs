﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebAPIGestionBancaire.Models
{
    public class Debit
    {
        public int idCompte { get; set; }
        public decimal montant { get; set; }
    }
}
