using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace HClassLibrary
{
    public partial class HTabPageEx : System.Windows.Forms.TabPage
    {
        public HTabPageEx()
        {
            InitializeComponent();
        }

        public HTabPageEx(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
        }
    }
}
