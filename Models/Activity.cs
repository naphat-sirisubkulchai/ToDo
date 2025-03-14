using System;
using System.Collections.Generic;

namespace ToDo.Models
{
    public partial class Activity
    {
        public uint Id { get; set; }
        public uint UserId { get; set; }
        public string Name { get; set; } = null!;
        public DateTime When { get; set; }

        public virtual User User { get; set; } = null!;
    }
}
