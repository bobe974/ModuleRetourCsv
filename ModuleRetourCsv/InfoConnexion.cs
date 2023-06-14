using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModuleRetourCsv
{
    class FtpInfoConnexion
    {
        private string path, user, password;

        public FtpInfoConnexion(string uploadPath, string user, string password)
        {
            this.path = uploadPath;
            this.user = user;
            this.password = password;
        }

        public string Path
        {
            get { return path; }
            set { path = value; }
        }

        public string User
        {
            get { return user; }
            set { user = value; }
        }

        public string Password
        {
            get { return password; }
            set { password = value; }
        }
    }

}
