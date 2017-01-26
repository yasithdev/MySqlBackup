using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace MySqlBackupTestApp
{
    public partial class FormDecryptOldDumpFile : Form
    {
        public FormDecryptOldDumpFile()
        {
            InitializeComponent();
        }

        private void btOpen_Click(object sender, EventArgs e)
        {
            var f = new OpenFileDialog();
            f.Multiselect = false;
            if (f.ShowDialog() != DialogResult.OK)
                return;

            txtSourceFile.Text = f.FileName;
        }

        private void btSaveAs_Click(object sender, EventArgs e)
        {
            var f = new SaveFileDialog();
            if (f.ShowDialog() != DialogResult.OK)
                return;

            txtOutputFile.Text = f.FileName;
        }

        private void btStart_Click(object sender, EventArgs e)
        {
            try
            {
                DecryptSqlDumpFile(txtSourceFile.Text, txtOutputFile.Text, txtPwd.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void DecryptSqlDumpFile(string originalFile, string newFile, string encryptionKey)
        {
            encryptionKey = Sha2Hash(encryptionKey);
            var saltSize = GetSaltSize(encryptionKey);

            if (!File.Exists(originalFile))
                throw new Exception("Original file is not exists.");

            var utf8WithoutBOM = new UTF8Encoding(false);

            using (TextReader textReader = new StreamReader(originalFile, utf8WithoutBOM))
            {
                if (File.Exists(newFile))
                    File.Delete(newFile);

                var line = "";

                using (TextWriter textWriter = new StreamWriter(newFile, false, utf8WithoutBOM))
                {
                    while (line != null)
                    {
                        line = textReader.ReadLine();
                        if (line == null)
                            break;
                        line = DecryptWithSalt(line, encryptionKey, saltSize);
                        if (line.StartsWith("-- ||||"))
                            line = "";

                        textWriter.WriteLine(line);
                    }
                }
            }
        }

        private string Sha2Hash(string input)
        {
            var ba = Encoding.UTF8.GetBytes(input);
            return Sha2Hash(ba);
        }

        private string Sha2Hash(byte[] ba)
        {
            var sha2 = new SHA256Managed();
            var ba2 = sha2.ComputeHash(ba);
            return BitConverter.ToString(ba2).Replace("-", string.Empty).ToLower();
        }

        private int GetSaltSize(string key)
        {
            var a = key.GetHashCode();
            var b = Convert.ToString(a);
            var ca = b.ToCharArray();
            var c = 0;
            foreach (var cc in ca)
                if (char.IsNumber(cc))
                    c += Convert.ToInt32(cc.ToString());
            return c;
        }

        private string DecryptWithSalt(string input, string key, int saltSize)
        {
            try
            {
                var salt = input.Substring(0, saltSize);
                var data = input.Substring(saltSize);
                return AES_Decrypt(data, key + salt);
            }
            catch
            {
                throw new Exception("Incorrect password or incomplete context.");
            }
        }

        private string AES_Decrypt(string input, string password)
        {
            var cipherBytes = Convert.FromBase64String(input);
            var pdb = new PasswordDeriveBytes(password,
                new byte[]
                {
                    0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65,
                    0x64, 0x76, 0x65, 0x64, 0x65, 0x76
                });
            var decryptedData = AES_Decrypt(cipherBytes, pdb.GetBytes(32), pdb.GetBytes(16));
            return Encoding.UTF8.GetString(decryptedData);
        }

        private byte[] AES_Decrypt(byte[] cipherData, byte[] Key, byte[] IV)
        {
            using (var ms = new MemoryStream())
            {
                var alg = Rijndael.Create();
                alg.Key = Key;
                alg.IV = IV;
                using (var cs = new CryptoStream(ms, alg.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(cipherData, 0, cipherData.Length);
                    cs.Close();
                    var decryptedData = ms.ToArray();
                    return decryptedData;
                }
            }
        }
    }
}