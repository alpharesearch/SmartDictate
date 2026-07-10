using System;

namespace SmartDictateAI
{
    public class VocabularyReplacement
    {
        public string Target { get; set; } = string.Empty;
        public string Replacement { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{Target} -> {Replacement}";
        }
    }
}
