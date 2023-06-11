using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebAPIGestionBancaire.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComptesController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public ComptesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET: api/<ComptesController>
        [HttpGet]
        public JsonResult Get()
        {
            string query = @"select * from compte;";
            DataTable table = new DataTable();
            string sqlDataSource = _configuration.GetConnectionString("CompteAppConn");
            NpgsqlDataReader myReader;
            using (NpgsqlConnection myCon = new NpgsqlConnection(sqlDataSource))
            {
                myCon.Open();
                using (NpgsqlCommand myCommand = new NpgsqlCommand(query, myCon))
                {
                    myReader = myCommand.ExecuteReader();
                    table.Load(myReader);

                    myReader.Close();
                    myCon.Close();

                }
            }

            return new JsonResult(table);
        
        }

        // GET api/<ComptesController>/5
        [HttpGet("{id}")]
        public JsonResult Get(int id)
        {
            string query = @"
                select * from compte
                where ID_Compte=@CompteId 
            ";

            DataTable table = new DataTable();
            string sqlDataSource = _configuration.GetConnectionString("CompteAppConn");
            NpgsqlDataReader myReader;
            using (NpgsqlConnection myCon = new NpgsqlConnection(sqlDataSource))
            {
                myCon.Open();
                using (NpgsqlCommand myCommand = new NpgsqlCommand(query, myCon))
                {
                    myCommand.Parameters.AddWithValue("@CompteId", id);
                    myReader = myCommand.ExecuteReader();
                    table.Load(myReader);

                    myReader.Close();
                    myCon.Close();

                }
            }

            return new JsonResult(table);
        }

        // POST api/<ComptesController>
        [HttpPost]
        public JsonResult Post(Models.Compte cmp)
        {
            string query = @"
                insert into compte (Nom, Solde) VALUES (@Nom, @Solde)
            ";

            DataTable table = new DataTable();
            string sqlDataSource = _configuration.GetConnectionString("CompteAppConn");
            NpgsqlDataReader myReader;
            using (NpgsqlConnection myCon = new NpgsqlConnection(sqlDataSource))
            {
                myCon.Open();
                using (NpgsqlCommand myCommand = new NpgsqlCommand(query, myCon))
                {
                    myCommand.Parameters.AddWithValue("@Nom", cmp.Nom);
                    myCommand.Parameters.AddWithValue("@Solde", cmp.Solde);
                    myReader = myCommand.ExecuteReader();
                    table.Load(myReader);

                    myReader.Close();
                    myCon.Close();

                }
            }

            return new JsonResult("Added Successfully");
        }

        

        // DELETE api/<ComptesController>/5
        [HttpDelete("{id}")]
        public JsonResult Delete(int id)
        {
            string queryDeleteMouvement = @"
    delete from mouvement
    where ID_Compte_Destinataire = @CompteId
";

            string queryDeleteCompte = @"
    delete from compte
    where ID_Compte = @CompteId
";

            string sqlDataSource = _configuration.GetConnectionString("CompteAppConn");

            using (NpgsqlConnection myCon = new NpgsqlConnection(sqlDataSource))
            {
                myCon.Open();

                using (NpgsqlCommand deleteMouvementCommand = new NpgsqlCommand(queryDeleteMouvement, myCon))
                {
                    deleteMouvementCommand.Parameters.AddWithValue("@CompteId", id);
                    deleteMouvementCommand.ExecuteNonQuery();
                }

                using (NpgsqlCommand deleteCompteCommand = new NpgsqlCommand(queryDeleteCompte, myCon))
                {
                    deleteCompteCommand.Parameters.AddWithValue("@CompteId", id);
                    deleteCompteCommand.ExecuteNonQuery();
                }

                myCon.Close();
            }

            return new JsonResult("Deleted Successfully");

        }

        [HttpPost("debit")]
        public IActionResult EffectuerDebit(Models.Debit debit)
        {
            // Récupérer le compte à partir de la base de données
            string compteQuery = "SELECT * FROM compte WHERE ID_Compte = @CompteId;";
            Models.Compte compte = null;
            string sqlDataSource = _configuration.GetConnectionString("CompteAppConn");
            using (NpgsqlConnection connection = new NpgsqlConnection(sqlDataSource))
            {
                connection.Open();

                using (NpgsqlCommand compteCommand = new NpgsqlCommand(compteQuery, connection))
                {
                    compteCommand.Parameters.AddWithValue("@CompteId", debit.idCompte);
                    using (NpgsqlDataReader compteReader = compteCommand.ExecuteReader())
                    {
                        if (compteReader.Read())
                        {
                            compte = new Models.Compte
                            {
                                ID_compte = compteReader.GetInt32(0),
                                Solde = compteReader.GetDecimal(2)
                            };
                        }
                        else
                        {
                            return NotFound("Compte non trouvé.");
                        }
                    }
                }

                if (compte.Solde < debit.montant)
                {
                    return BadRequest("Solde insuffisant pour effectuer le débit.");
                }

                compte.Solde -= debit.montant;

                // Mettre à jour le solde du compte dans la base de données
                string updateQuery = "UPDATE compte SET Solde = @NouveauSolde WHERE ID_Compte = @CompteId;";

                using (NpgsqlCommand updateCommand = new NpgsqlCommand(updateQuery, connection))
                {
                    updateCommand.Parameters.AddWithValue("@NouveauSolde", compte.Solde);
                    updateCommand.Parameters.AddWithValue("@CompteId", compte.ID_compte);
                    updateCommand.ExecuteNonQuery();
                }

                // Enregistrer le mouvement dans la table "mouvement"
                string insertMouvementQuery = "INSERT INTO mouvement (ID_Compte, Type_Mouvement, Montant, Date) VALUES (@CompteId, 'Débit', @Montant, @Date);";

                using (NpgsqlCommand insertMouvementCommand = new NpgsqlCommand(insertMouvementQuery, connection))
                {
                    insertMouvementCommand.Parameters.AddWithValue("@CompteId", compte.ID_compte);
                    insertMouvementCommand.Parameters.AddWithValue("@Montant", debit.montant);
                    insertMouvementCommand.Parameters.AddWithValue("@Date", DateTime.Now);
                    insertMouvementCommand.ExecuteNonQuery();
                }

                connection.Close();
            }

            return Ok("Débit effectué avec succès.");
        }

        [HttpPost("credit")]
        public IActionResult EffectuerCredit(Models.Debit debit)
        {
            // Récupérer le compte à partir de la base de données
            string compteQuery = "SELECT * FROM compte WHERE ID_Compte = @CompteId;";
            Models.Compte compte = null;
            string sqlDataSource = _configuration.GetConnectionString("CompteAppConn");
            using (NpgsqlConnection connection = new NpgsqlConnection(sqlDataSource))
            {
                connection.Open();

                using (NpgsqlCommand compteCommand = new NpgsqlCommand(compteQuery, connection))
                {
                    compteCommand.Parameters.AddWithValue("@CompteId", debit.idCompte);
                    using (NpgsqlDataReader compteReader = compteCommand.ExecuteReader())
                    {
                        if (compteReader.Read())
                        {
                            compte = new Models.Compte
                            {
                                ID_compte = compteReader.GetInt32(0),
                                Solde = compteReader.GetDecimal(2)
                            };
                        }
                        else
                        {
                            return NotFound("Compte non trouvé.");
                        }
                    }
                }

                compte.Solde += debit.montant;

                // Mettre à jour le solde du compte dans la base de données
                string updateQuery = "UPDATE compte SET Solde = @NouveauSolde WHERE ID_Compte = @CompteId;";

                using (NpgsqlCommand updateCommand = new NpgsqlCommand(updateQuery, connection))
                {
                    updateCommand.Parameters.AddWithValue("@NouveauSolde", compte.Solde);
                    updateCommand.Parameters.AddWithValue("@CompteId", compte.ID_compte);
                    updateCommand.ExecuteNonQuery();
                }

                // Enregistrer le mouvement dans la table "mouvement"
                string insertMouvementQuery = "INSERT INTO mouvement (ID_Compte, Type_Mouvement, Montant, Date) VALUES (@CompteId, 'Crédit', @Montant, @Date);";

                using (NpgsqlCommand insertMouvementCommand = new NpgsqlCommand(insertMouvementQuery, connection))
                {
                    insertMouvementCommand.Parameters.AddWithValue("@CompteId", compte.ID_compte);
                    insertMouvementCommand.Parameters.AddWithValue("@Montant", debit.montant);
                    insertMouvementCommand.Parameters.AddWithValue("@Date", DateTime.Now);
                    insertMouvementCommand.ExecuteNonQuery();
                }

                connection.Close();
            }

            return Ok("Crédit effectué avec succès.");
        }

        [HttpPost("virement")]
        public IActionResult EffectuerVirement(Models.Virement vir)
        {
            // Récupérer les comptes source et destinataire à partir de la base de données
            string compteSourceQuery = "SELECT * FROM compte WHERE ID_Compte = @CompteId;";
            string compteDestinataireQuery = "SELECT * FROM compte WHERE ID_Compte = @CompteId;";
            Models.Compte compteSource = null;
            Models.Compte compteDestinataire = null;
            string sqlDataSource = _configuration.GetConnectionString("CompteAppConn");
            using (NpgsqlConnection connection = new NpgsqlConnection(sqlDataSource))
            {
                connection.Open();

                // Récupérer le compte source
                using (NpgsqlCommand compteSourceCommand = new NpgsqlCommand(compteSourceQuery, connection))
                {
                    compteSourceCommand.Parameters.AddWithValue("@CompteId", vir.idCompteSource);
                    using (NpgsqlDataReader compteSourceReader = compteSourceCommand.ExecuteReader())
                    {
                        if (compteSourceReader.Read())
                        {
                            compteSource = new Models.Compte
                            {
                                ID_compte = compteSourceReader.GetInt32(0),
                                Solde = compteSourceReader.GetDecimal(2)
                            };
                        }
                        else
                        {
                            return NotFound("Compte source non trouvé.");
                        }
                    }
                }

                // Récupérer le compte destinataire
                using (NpgsqlCommand compteDestinataireCommand = new NpgsqlCommand(compteDestinataireQuery, connection))
                {
                    compteDestinataireCommand.Parameters.AddWithValue("@CompteId", vir.idCompteDestinataire);
                    using (NpgsqlDataReader compteDestinataireReader = compteDestinataireCommand.ExecuteReader())
                    {
                        if (compteDestinataireReader.Read())
                        {
                            compteDestinataire = new Models.Compte
                            {
                                ID_compte = compteDestinataireReader.GetInt32(0),
                                Solde = compteDestinataireReader.GetDecimal(2)
                            };
                        }
                        else
                        {
                            return NotFound("Compte destinataire non trouvé.");
                        }
                    }
                }

                if (compteSource.Solde < vir.montant)
                {
                    return BadRequest("Solde insuffisant pour effectuer le virement.");
                }

                compteSource.Solde -= vir.montant;
                compteDestinataire.Solde += vir.montant;

                // Mettre à jour les soldes des comptes dans la base de données
                string updateCompteQuery = "UPDATE compte SET Solde = @NouveauSolde WHERE ID_Compte = @CompteId;";

                using (NpgsqlCommand updateCompteCommand = new NpgsqlCommand(updateCompteQuery, connection))
                {
                    // Mettre à jour le solde du compte source
                    updateCompteCommand.Parameters.AddWithValue("@NouveauSolde", compteSource.Solde);
                    updateCompteCommand.Parameters.AddWithValue("@CompteId", compteSource.ID_compte);
                    updateCompteCommand.ExecuteNonQuery();

                    // Mettre à jour le solde du compte destinataire
                    updateCompteCommand.Parameters.Clear();
                    updateCompteCommand.Parameters.AddWithValue("@NouveauSolde", compteDestinataire.Solde);
                    updateCompteCommand.Parameters.AddWithValue("@CompteId", compteDestinataire.ID_compte);
                    updateCompteCommand.ExecuteNonQuery();
                }

                // Enregistrer les mouvements dans la table "mouvement"
                string insertMouvementQuery = "INSERT INTO mouvement (ID_Compte, Type_Mouvement,ID_Compte_Destinataire, Montant, Date) VALUES (@CompteId, 'Virement',@CompteIdDest, @Montant, @Date);";
                using (NpgsqlCommand insertMouvementCommand = new NpgsqlCommand(insertMouvementQuery, connection))
                {
                    insertMouvementCommand.Parameters.AddWithValue("@CompteId", compteSource.ID_compte);
                    insertMouvementCommand.Parameters.AddWithValue("@CompteIdDest", compteDestinataire.ID_compte);
                    insertMouvementCommand.Parameters.AddWithValue("@Montant", vir.montant);
                    insertMouvementCommand.Parameters.AddWithValue("@Date", DateTime.Now);
                    insertMouvementCommand.ExecuteNonQuery();
                }

 

                connection.Close();
            }

            return Ok("Virement effectué avec succès.");
        }

        [HttpGet("mouvements/{idCompte}")]
        public IActionResult GetMouvementsCompte(int idCompte)
        {
            string query = "SELECT * FROM mouvement WHERE ID_Compte = @CompteId;";
            List<Models.Mouvement> mouvements = new List<Models.Mouvement>();
            string sqlDataSource = _configuration.GetConnectionString("CompteAppConn");
            using (NpgsqlConnection connection = new NpgsqlConnection(sqlDataSource))
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CompteId", idCompte);

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Models.Mouvement mouvement = new Models.Mouvement
                            {
                                ID_mouvement = reader.GetInt32(0),
                                ID_compte = reader.GetInt32(1),
                                ID_compte_destinataire = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                                Type_mouvement = reader.GetString(3),
                                Montant = reader.GetDecimal(4),
                                Date = reader.GetDateTime(5)
                            };

                            mouvements.Add(mouvement);
                        }
                    }
                }

                connection.Close();
            }

            return Ok(mouvements);
        }



    }
}
