using ProjetoZ.Domian.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoZ.Application.DTOs
{
    public class UserDto
    {
        public Guid Id { get; set; }

        public SteamProfile Profile { get; set; } = new();

        public int Coins { get; set; }
    }
}
