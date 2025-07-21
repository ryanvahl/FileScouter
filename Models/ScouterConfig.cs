using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace FileScouter.Models
{
    // should this be a static class
    internal class ScouterConfig
    {
        public XElement? Config { get; set; }
        public XElement? Paths { get; set; }
        public string? StartFolder { get; set; }
        public string? EndFolder { get; set; }
        public string? LogFile { get; set; }
    }
}