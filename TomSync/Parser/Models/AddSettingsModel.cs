using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TomSync.Models;

namespace TomSync.Parser.Models
{
    public class AddSettingsModel
    {
        public SpeedLimiter SpeedLimiter { get; set; }
        public AddSettingsModel()
        {
            SpeedLimiter = new SpeedLimiter();
        }
    }
}
