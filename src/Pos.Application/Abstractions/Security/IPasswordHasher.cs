using System;
using System.Collections.Generic;
using System.Text;

namespace Pos.Application.Abstractions.Security
{
    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string password, string hash);
    }
}
