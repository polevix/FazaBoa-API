using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FazaBoa_API.Dtos
{
    public class GroupCreationDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool HasUniqueRewards { get; set; } 
        public IFormFile? Photo { get; set; } // Campo para o upload da foto do grupo (opcional)
    }
}
