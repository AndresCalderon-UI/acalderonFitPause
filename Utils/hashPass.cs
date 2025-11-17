using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

public static class hashPass
{
    public static string HashSHA256(string _pass)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(_pass);
            byte[] hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }
}

