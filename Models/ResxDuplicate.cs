﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace resource_merger.Models
{
    internal class ResxDuplicate
    {
        public List<string> Keys { get; set; } = new List<string>();
        public string Value { get; set; }
    }
}
