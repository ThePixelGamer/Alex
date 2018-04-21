﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace Alex.API.Gui
{
    public interface IGuiScreen : IGuiElement3D, IGuiFocusContext
    {

        void UpdateLayout();

    }
}
