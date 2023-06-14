using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModuleRetourCsv
{
    class Program
    {
        public static string inifilePath = Path.Combine(Directory.GetCurrentDirectory(), "config.ini");
        //fichier log
        public static Logger logger;

        public static void Main(string[] args)
        {
            Console.WriteLine(inifilePath);
            //création du fichier log
            logger = new Logger();

            logger.WriteToLog("**************Début du programme***************");

            if (File.Exists(inifilePath))
            {
                string[] lines = File.ReadAllLines(inifilePath);
            }
            else //création d'un modele du fichier INI
            {
                Console.WriteLine("Le fichier INI n'existe pas, création du modele...");
                logger.WriteToLog("Le fichier INI n'existe pas, création du modele...");

                IniData ini = new IniData();

                ini.Sections.AddSection("FtpStock");
                ini["FtpStock"].AddKey("UploadPath", "xxxx");
                ini["FtpStock"].AddKey("User", "xxxx");
                ini["FtpStock"].AddKey("Password", "xxxx");

                ini.Sections.AddSection("FtpRedisma");
                ini["FtpRedisma"].AddKey("UploadPath", "xxxx");
                ini["FtpRedisma"].AddKey("User", "xxxx");
                ini["FtpRedisma"].AddKey("Password", "xxxx");

                ini.Sections.AddSection("FtpDisbep");
                ini["FtpDisbep"].AddKey("UploadPath", "xxxx");
                ini["FtpDisbep"].AddKey("User", "xxxx");
                ini["FtpDisbep"].AddKey("Password", "xxxx");

                //emplacement où seront stockés les fichiers json téléchargés
                ini.Sections.AddSection("Répertoirelocal");
                ini["RépertoirelocalStock"].AddKey("StockcsvFolderPath", "xxxx");
                ini["RépertoirelocalStock"].AddKey("RedismacsvFolderPath", "xxxx");
                ini["RépertoirelocalStock"].AddKey("DisbepcsvFolderPath", "xxxx");


                ini.Sections.AddSection("DatabaseSqlServer");

                ini["DatabaseSqlServer"].AddKey("ServerName", "xxxx");

                ini["DatabaseSqlServer"].AddKey("DbStock", "xxxx");
                ini["DatabaseSqlServer"].AddKey("DbRedisma", "xxxx");
                ini["DatabaseSqlServer"].AddKey("DbDisbep", "xxxx");

                ini["DatabaseSqlServer"].AddKey("User", "xxxx");
                ini["DatabaseSqlServer"].AddKey("Password", "xxxx");

                //création du fichier
                FileIniDataParser fileParser = new FileIniDataParser();
                fileParser.WriteFile(inifilePath, ini);
            }

            //Création d'un objet parser pour lire le fichier INI
            var parser = new FileIniDataParser();
            //vérifie si le fichier INI existe puis lecture du fichier
            if (!File.Exists(inifilePath))
            {
                logger.WriteToLog("le fichier INI n'existe pas");
                throw new ArgumentException("le fichier INI n'existe pas");
            }

            IniData data = parser.ReadFile(inifilePath);
            Console.WriteLine("lecture du fichier ini...");
            logger.WriteToLog("lecture du fichier ini...");

            // Récupération des informations de connexion à partir du fichier INI
            FtpInfoConnexion ftpStock = new FtpInfoConnexion(data["FtpStock"]["UploadPath"], data["FtpStock"]["User"], data["FtpStock"]["Password"]);
            FtpInfoConnexion ftpRedisma = new FtpInfoConnexion(data["FtpRedisma"]["UploadPath"], data["FtpRedisma"]["User"], data["FtpRedisma"]["Password"]);
            FtpInfoConnexion ftpDisbep = new FtpInfoConnexion(data["FtpDisbep"]["UploadPath"], data["FtpDisbep"]["User"], data["FtpDisbep"]["Password"]);

            List<FtpInfoConnexion> LftpInfo = new List<FtpInfoConnexion>();
            LftpInfo.Add(ftpStock);
            LftpInfo.Add(ftpRedisma);
            LftpInfo.Add(ftpDisbep);

            //base de données
            string servername = data["DatabaseSqlServer"]["ServerName"];
            string sqlServerDb1 = data["DatabaseSqlServer"]["DbStock"];
            string sqlServerDb2 = data["DatabaseSqlServer"]["DbRedisma"];
            string sqlServerDb3 = data["DatabaseSqlServer"]["DbDisbep"];

            string sqlServerUser = data["DatabaseSqlServer"]["User"];
            string sqlServerPwd = data["DatabaseSqlServer"]["Password"];

            //répertoire local
            string StockcsvFolderPath = data["Répertoirelocal"]["StockcsvFolderPath"];
            string RedismacsvFolderPath = data["Répertoirelocal"]["RedismacsvFolderPath"];
            string DisbepcsvFolderPath = data["Répertoirelocal"]["DisbepcsvFolderPath"];

            List<String> LFolderPath = new List<string>();
            LFolderPath.Add(StockcsvFolderPath);
            LFolderPath.Add(RedismacsvFolderPath);
            LFolderPath.Add(DisbepcsvFolderPath);

            List<String> Lserver = new List<string>();
            Lserver.Add(sqlServerDb1);
            Lserver.Add(sqlServerDb2);
            Lserver.Add(sqlServerDb3);

            int i = 0;
            foreach (string dbname in Lserver)
            {

                //vérifie que le dossier local existe sinon création du répertoire
                if 
                    (!(Directory.Exists(LFolderPath[i])))
                {
                    Console.WriteLine("le répertoire" + LFolderPath[i] + "n'existe pas");
                    Directory.CreateDirectory(LFolderPath[i]);
                    Console.WriteLine("le répertoire" + LFolderPath[i] + "a été créer");

                }

                //Extraction des données et Export au format .CSV
                Console.WriteLine("");
                Console.WriteLine("");
                Console.WriteLine("***************************Extraction sur le serveur " + dbname + "***************************");
                logger.WriteToLog("***************************Connexion Sql Server sur" + dbname + "***************************");
                SqlManager sqlManager = new SqlManager(servername, dbname, sqlServerUser, sqlServerPwd);
                Dictionary<string, string> queriesAndFiles = new Dictionary<string, string>() {
        {
          "SELECT ar_ref as code_article,ar_codebarre as ean,ar_design as libelle,fa_codefamille as code_famille,AR_PrixVen as puht1, colisage as pcb FROM F_ARTICLE",
           Path.Combine(LFolderPath[i],"Article.csv")
        }, {
          "SELECT ct_num as code_client,ct_intitule as libelle,ct_adresse as adresse1,ct_complement as adresse2,ct_codepostal as codepostal,ct_ville as ville, N_CatTarif as tarif,'' as code_secteur, '' as catcomptable, '' as mode_reglement FROM F_COMPTET WHERE CT_Type=0",
           Path.Combine(LFolderPath[i],"Client.csv")
        }, {
          "SELECT '01' AS code_depot,DE_Intitule AS nom, DE_No AS refext FROM F_DEPOT WHERE De_principal = 1",
           Path.Combine(LFolderPath[i],"Depot.csv")
        }, {
          "SELECT FA_CODEFAMILLE as code_famille, FA_INTITULE as libelle, 'A' as application FROM F_FAMILLE WHERE fa_central is not null or fa_central != '' ORDER BY FA_CODEFAMILLE ASC",
          Path.Combine(LFolderPath[i]," Famille.csv")
        }, {
          "select ct_num as code_client,fa_codefamille as code_famille,fc_remise as taux_remise from F_FAMCLIENT",
           Path.Combine(LFolderPath[i],"RemisesFamillesClients.csv")
        }, {
          "SELECT AS_QteSto AS quantite, '01' AS code_depot, F_ARTSTOCK.AR_REF AS code_article, FORMAT(F_ARTSTOCK.cbModification, 'yyyyMMddhhmmss') AS date FROM F_ARTSTOCK, F_DEPOT WHERE F_ARTSTOCK.DE_No = F_DEPOT.DE_No AND DE_Principal = 1",
          Path.Combine(LFolderPath[i],"Stock.csv")
        }, {
          "SELECT fa.ar_ref as code_article, fa.ac_categorie as num_categorie_tarifaire, pc.CT_Intitule as nom_categorie_tarifaire, fa.ac_prixven as prix_vente, fa.ac_remise as remise from F_ARTCLIENT fa join P_CATTARIF pc on pc.cbIndice = fa.AC_Categorie",
           Path.Combine(LFolderPath[i],"TarifsCategoriesTarifaires.csv")
        }, {
          "SELECT ARTCLI.AR_REF as code_article,CLI.CT_NUM AS code_client, CLI.CT_Intitule as nom_client,ARTCLI.AC_PRIXVEN AS prix, ARTCLI.AC_REMISE AS remise FROM F_ARTCLIENT ARTCLI JOIN F_COMPTET CLI ON CLI.CT_NUM = ARTCLI.CT_Num WHERE ARTCLI.AC_remise > 0 AND CLI.CT_NumCentrale IS NULL",
           Path.Combine(LFolderPath[i],"TarifsClients.csv")
        }, {
          "SELECT AR_REF as code_article, F_ARTCOMPTA.ACP_Champ as categorie_comptable, F_ARTCOMPTA.ACP_ComptaCPT_Taxe1 as code, F_taxe.TA_Intitule as intitule_tva, ta_taux as taux_tva FROM F_TAXE,F_ARTCOMPTA WHERE F_ARTCOMPTA.ACP_COMPTACPT_TAXE1=F_TAXE.TA_CODE AND ACP_CHAMP=1 AND ACP_TYPE=0",
          Path.Combine(LFolderPath[i],"TvaArticles.csv")
        }, {
          "SELECT FA_CODEFAMILLE as code_famille,F_FAMCOMPTA.FCP_Champ as cnum_categorie_comptable, F_FAMCOMPTA.FCP_ComptaCPT_Taxe1 as code, f_taxe.ta_intitule as intitule_tva, ta_taux as taux_tva from F_TAXE, F_FAMCOMPTA WHERE F_FAMCOMPTA.FCP_COMPTACPT_TAXE1 = F_TAXE.TA_CODE AND FCP_Type = 0 AND FCP_Champ = 1",
          Path.Combine(LFolderPath[i],"TvaFamilles.csv")
        }
      };
                try
                {
                    sqlManager.GenerateCsvFiles(sqlManager.ExecuteSqlQueries(queriesAndFiles), queriesAndFiles);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
 
                    logger.WriteToLog($"Impossible de générer les fichiers CSV: {e}");
                }

                Console.WriteLine("*******Upload sur le ftp*********");
                logger.WriteToLog("*******Upload sur le ftp*********");

                //Connexion au serveur par FTP
                FTPManager FtpUpload = new FTPManager(LftpInfo[i].User, LftpInfo[i].Password, LftpInfo[i].Path);

                //lecture de tous les fichiers csv en local 
                foreach (string csvFilePath in Directory.GetFiles(LFolderPath[i], "*.csv"))
                {
                    Console.Write(csvFilePath);
                    //envoie sur le serveur 
                    FtpUpload.UploadFileToFtp(csvFilePath);
                }
                i++;
                Console.WriteLine("***************************FIN de l'extraction sur le serveur " + dbname + "***************************");
                Console.WriteLine("");
            }
            Console.WriteLine("");
            Console.WriteLine("******************************Fin du programme*************************************");
            logger.WriteToLog("******************************Fin du programme*************************************");

        }
    }
}



