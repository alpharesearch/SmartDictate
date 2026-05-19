using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartDictateAI
{
    public class PromptProfile
    {
        public string Name { get; set; } = "Default";
        public string SystemPrompt { get; set; } = "";
        public string UserPrompt { get; set; } = "";

        // Overriding ToString helps WinForms ComboBox easily display the name
        public override string ToString()
        {
            return Name;
        }
    }

}
