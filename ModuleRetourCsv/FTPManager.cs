using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ModuleRetourCsv
{
    class FTPManager
    {
        private FtpWebRequest ftpRequest;
        private String user;
        private String pwd;
        private String url;
        Logger logger = new Logger();

        public FTPManager(String user, String pwd, String url)
        {
            this.user = user;
            this.pwd = pwd;
            this.url = url;
        }

        //TODO Une instance de ftpRequest pour toutes les opérations 

        /**
         * Etabli une connexion FTP puis synchonise les fichiers du répertoire local au répertoire distant
         * Télécharge les fichiers qui n'existe pas en local ou qui ont été mise à jours
         */
        public void SyncFilesFromFtp(string localFolderPath)
        {
            Console.WriteLine("Tentative de connexion au serveur par protocole FTP...\n");
            try
            {
                //établir une connexion FTP
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(url);
                request.Credentials = new NetworkCredential(user, pwd);
                request.Method = WebRequestMethods.Ftp.ListDirectory;

                //vérifie si la connexion par FTP fonctionne
                FtpWebResponse response = null;
                bool isConnected = false;
                int nbEssai = 0;
                int maxEssai = 5;
                while (!isConnected)
                {
                    try
                    {
                        //TODO pas optimal mais ca marche
                        //établir une connexion FTP
                        FtpWebRequest newrequest = (FtpWebRequest)WebRequest.Create(url);
                        request = newrequest;
                        request.Credentials = new NetworkCredential(user, pwd);
                        request.Method = WebRequestMethods.Ftp.ListDirectory;
                        response = (FtpWebResponse)request.GetResponse();
                        Console.WriteLine($"connexion FTP établie à {url}\n");
                        isConnected = true;
                    }
                    catch (WebException e)
                    {
                        //Attendre 5 secondes avant de retenter la connexion
                        Thread.Sleep(5000);
                        Console.WriteLine("pas de connexion FTP, nouvelle tentative dans 5s");
                        logger.WriteToLog("pas de connexion FTP, nouvelle tentative dans 5s");
                        Console.WriteLine("erreur -> " + e + "\n");
                        logger.WriteToLog("erreur -> " + e + "\n");
                        Console.WriteLine($"nombre d'essai {nbEssai + 1}/{maxEssai}");
                        logger.WriteToLog($"nombre d'essai {nbEssai + 1}/{maxEssai}");
                    }

                    nbEssai++;

                    if (nbEssai == maxEssai)
                    {
                        Console.WriteLine("Le nombre maximal de tentatives de connexion a été atteint.");
                        // Arrêter le programme 
                        Environment.Exit(0);
                    }
                }

                //liste des fichiers distants
                List<string> remoteFiles = new List<string>();
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string line = reader.ReadLine();
                    Console.WriteLine("liste des fichiers trouvés sur le serveur FTP: \n");

                    while (!string.IsNullOrEmpty(line))  //pas de fichiers vide ou qui ne sont pas des .json
                    {
                        Console.WriteLine(line);
                        if (!(IsNotJson(line))) //si c'est un json on l'ajoute à la liste des téléchargements
                        {
                            String filename = Path.GetFileName(response.ResponseUri.AbsolutePath + line);
                            remoteFiles.Add(response.ResponseUri.AbsolutePath + "/" + filename);
                            Console.WriteLine(response.ResponseUri.AbsolutePath + "/" + filename);
                        }
                        line = reader.ReadLine(); //lecture de la ligne suivante
                    }
                }

                Console.WriteLine("");
                //liste des fichiers locaux
                List<string> localFiles = Directory.GetFiles(localFolderPath).ToList();

                //synchroniser les fichiers
                foreach (string remoteFilePath in remoteFiles)
                {
                    string fileName = Path.GetFileName(remoteFilePath);
                    string localFilePath = Path.Combine(localFolderPath, fileName); //path dossier local + nom fichier distant    

                    if (localFiles.Contains(localFilePath))
                    {
                        long remoteFileSize = GetFileSize(request, fileName);
                        long localFileSize = new FileInfo(localFilePath).Length;
                        Console.WriteLine("taille du fichier local: " + localFileSize);
                        Console.WriteLine("taille du fichier distant: " + remoteFileSize);
                        if (remoteFileSize != localFileSize)
                        {
                            //télécharger la version la plus récente du fichier
                            Console.WriteLine("Fichier existant, dl de la version la plus récente de: " + fileName);
                            DownloadFileFromFtp(request, fileName, localFilePath);
                        }
                    }
                    else
                    {
                        //cas d'un nouveau fichier, le télécharger
                        Console.WriteLine("Fichier inexistant, téléchargement de: " + fileName);
                        DownloadFileFromFtp(request, fileName, localFilePath);
                    }
                }
                response.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /**
         * Se connecte à un serveur par protocole FTP et télécharge le fichiers lié à l'url  
         */
        public void DownloadFileFromFtp(FtpWebRequest request, string fileUrl, string destPath)
        {
            Console.WriteLine("format de l'uri download: " + fileUrl);
            String path = this.url + "/" + fileUrl;
            Console.WriteLine("path: " + this.url);
            //créer une nouvelle requête pour télécharger le fichier spécifié
            FtpWebRequest downloadRequest = (FtpWebRequest)WebRequest.Create(path);
            downloadRequest.Method = WebRequestMethods.Ftp.DownloadFile;
            downloadRequest.Credentials = request.Credentials; // réattribue les paramatres de connexion

            //Télécharger le fichier et l'écrire dans le fichier local
            using (FtpWebResponse response = (FtpWebResponse)downloadRequest.GetResponse())
            {
                using (Stream remoteStream = response.GetResponseStream())
                {
                    using (FileStream localStream = new FileStream(destPath, FileMode.Create))
                    {
                        remoteStream.CopyTo(localStream);
                    }
                }
            }
            Console.WriteLine($"Téléchargement de {fileUrl} terminé.");
        }

        /**
         *
         */
        public long GetFileSize(FtpWebRequest request, string fileUrl)
        {
            String path = this.url + "/" + fileUrl;
            Console.WriteLine("file uri: " + path);
            FtpWebRequest sizeRequest = (FtpWebRequest)WebRequest.Create(path);
            sizeRequest.Credentials = new NetworkCredential(user, pwd);
            sizeRequest.Method = WebRequestMethods.Ftp.GetFileSize;
            sizeRequest.UseBinary = true;

            using (FtpWebResponse response = (FtpWebResponse)sizeRequest.GetResponse())
            {
                return response.ContentLength;
            }
        }

        public bool IsNotJson(string filePath)
        {
            string extension = Path.GetExtension(filePath);
            if (extension == ".json")
            {
                //Console.WriteLine(filePath + " est un json");
                return false;
            }
            else
            {
                //Console.WriteLine(filePath + " n'est pas un json");
                return true;
            }
        }

        public void UploadFileToFtp(string sourceFilePath)
        {
            if (File.Exists(sourceFilePath))
            {
                // Créez la demande de téléchargement FTP.
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(url + "/" + Path.GetFileName(sourceFilePath));
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(this.user, this.pwd);

                // Lire le contenu du fichier source et l'envoyer au serveur FTP.
                byte[] fileContents = File.ReadAllBytes(sourceFilePath);
                request.ContentLength = fileContents.Length;
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(fileContents, 0, fileContents.Length);
                }

                Console.WriteLine($"upload de {sourceFilePath} sur dans le répertoire FTP {url} terminé.");
                logger.WriteToLog($"upload de {sourceFilePath} sur dans le répertoire FTP {url} terminé.");
            }
            else
            {
                Console.WriteLine($"échec upload fichier: le fichier {sourceFilePath} n'existe pas");
                logger.WriteToLog($"échec upload fichier: le fichier {sourceFilePath} n'existe pas");
            }
        }
    }
}
