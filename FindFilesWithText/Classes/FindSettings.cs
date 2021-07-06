using System.Collections.Generic;

namespace UpdatePackages.Classes
{
    public record Section
    {
        public string FileMask { get; init; }
        public IEnumerable<string> Regular { get; init; }
    }

    public record FindSettings
    {
        public string Directory { get; init; }
        public IEnumerable<Section> Sections { get; init;
        }
    }
}