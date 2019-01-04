using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyVideoPlayer
{
    public class MainViewModel : BaseViewModel
    {
        #region Properties

        public bool IsRepeating { get; set; }

        public string CurrentFile { get; set; }

        #endregion //Properties

        #region Constructors

        public MainViewModel()
        {
            IsRepeating = true;
        }

        #endregion //Constructors
    }
}
